// UploadService.cs
// 
// Authors:
//       Ventsislav Mladenov
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2018 Ventsislav Mladenov, Javier Suárez Ruiz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
    /// Upload service.
    /// </summary>
    [ServiceResolver(typeof(UploadServiceResolver))]
	sealed class UploadService : TeamFoundationServerService
    {
        UploadService(Uri baseUri, string servicePath)
            : base(baseUri, servicePath)
        {

        }

        const string NewLine = "\r\n";
        const string Boundary = "----------------------------8e5m2D6l5Q4h6";
        const int ChunkSize = 512 * 1024; //Chunk Size 512 K
        static readonly string uncompressedContentType = "application/octet-stream";

        #region Implemented abstract members of TfsService

        public override XNamespace MessageNs
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IServiceResolver ServiceResolver
        {
            get
            {
                return new UploadServiceResolver();
            }
        }

        #endregion

        void CopyStream(Stream source, Stream destination)
        {
            byte[] buffer = new byte[ChunkSize];
            int cnt;

            while ((cnt = source.Read(buffer, 0, ChunkSize)) > 0)
            {
                destination.Write(buffer, 0, cnt);
            }

            destination.Flush();
        }

        void CopyBytes(byte[] source, Stream destination)
        {
            using (var memorySource = new MemoryStream(source))
            {
                CopyStream(memorySource, destination);
            }
        }

        /// <summary>
        /// Uploads a file.
        /// </summary>
        /// <param name="workspaceName">Workspace name.</param>
        /// <param name="workspaceOwner">Workspace owner.</param>
        /// <param name="item">Item.</param>
        public void UploadFile(string workspaceName, string workspaceOwner, CommitItem item)
        {
            var fileContent = File.ReadAllBytes(item.LocalPath);
            var fileHash = Hash(fileContent);
            string contentType = uncompressedContentType;

            using (var memory = new MemoryStream(fileContent))
            {
                byte[] buffer = new byte[ChunkSize];
                int cnt;

                while ((cnt = memory.Read(buffer, 0, ChunkSize)) > 0)
                {
                    var range = GetRange(memory.Position - cnt, memory.Position, fileContent.Length);
                    UploadPart(item.RepositoryPath, workspaceName, workspaceOwner, fileContent.Length, fileHash, range, contentType, buffer, cnt);
                }
            }
        }

        void AddContent(MultipartFormDataContent container, string value, string name, string fileName = "")
        {
            if (string.IsNullOrEmpty(fileName))
                container.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(value)), name);
            else
                container.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(value)), name, fileName);
        }

        WebResponse UploadPart(string fileName, string workspaceName, string workspaceOwner, int fileSize, string fileHash, string range, string contentType, byte[] bytes, int copyBytes)
        {
            var request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.AllowWriteStreamBuffering = true;
            request.ContentType = "multipart/form-data; boundary=" + Boundary.Substring(2);

            Server.Authorization.Authorize(request);

            var template = GetTemplate();
            var content = string.Format(template, fileName, workspaceName, workspaceOwner, fileSize, fileHash, range, "item", contentType);
            var contentBytes = Encoding.UTF8.GetBytes(content.Replace(Environment.NewLine, NewLine));

            using (var stream = new MemoryStream())
            {
                stream.Write(contentBytes, 0, contentBytes.Length);
                stream.Write(bytes, 0, copyBytes);
                var footContent = Encoding.UTF8.GetBytes(NewLine + Boundary + "--" + NewLine);
                stream.Write(footContent, 0, footContent.Length);
                stream.Flush();
                contentBytes = stream.ToArray();
            }

            using (var requestStream = request.GetRequestStream())
            {
                CopyBytes(contentBytes, requestStream);
            }

            var response = request.GetResponse();

            return response;
        }

        string GetTemplate()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("item");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{0}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("wsname");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{1}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("wsowner");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{2}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("filelength");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{3}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("hash");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{4}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("range");
            builder.AppendLine("\"");
            builder.AppendLine();
            builder.Append("{5}");
            builder.AppendLine();
            builder.AppendLine(Boundary);
            builder.Append("Content-Disposition: form-data; name=\"");
            builder.Append("content");
            builder.Append("\"; filename=\"");
            builder.Append("{6}");
            builder.AppendLine("\"");
            builder.Append("Content-Type: ");
            builder.Append("{7}");
            builder.AppendLine();
            builder.AppendLine();

            return builder.ToString();
        }
          
        string Hash(byte[] input)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                return Convert.ToBase64String(md5.ComputeHash(input));
            }
        }

        string GetRange(long start, long end, long length)
        {
            var builder = new StringBuilder(100);
            builder.Append("bytes=");
            builder.Append(start);
            builder.Append('-');
            builder.Append(end - 1);
            builder.Append('/');
            builder.Append(length);
            builder.AppendLine();

            return builder.ToString();
        }
    }
}