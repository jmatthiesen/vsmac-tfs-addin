// DownloadService.cs
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
using System.IO.Compression;
using System.Net;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
    /// Download service.
    /// </summary>
    [ServiceResolver(typeof(DownloadServiceResolver))]
	sealed class DownloadService : TeamFoundationServerService
    {
        DownloadService(Uri baseUri, string servicePath)
            : base(baseUri, servicePath)
        {

        }

        #region Implemented abstract members of TfsService

        public override System.Xml.Linq.XNamespace MessageNs => throw new NotImplementedException();

        public IServiceResolver ServiceResolver
        {
            get
            {
                return new DownloadServiceResolver();
            }
        }

        #endregion

        readonly Random random = new Random();

        LocalPath GetTempFileName(string extension)
        {
            var num = random.Next();
            var tempDir = Path.Combine(Path.GetTempPath(), "TfSAddinDownload");
          
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);
     
            return Path.Combine(tempDir, "tfsTemp" + num.ToString("X") + extension);    // Files are gzipped
        }
       /// <summary>
       /// Download file to temp folder.
       /// </summary>
       /// <returns>The to temp.</returns>
       /// <param name="artifactUri">Artifact URI.</param>
        public LocalPath DownloadToTemp(string artifactUri)
        {
            try
            {
                var client = new WebClient();
                Server.Authorization.Authorize(client);
                var tempFileName = GetTempFileName(".gz");
                var bulder = new UriBuilder(Url) { Query = artifactUri };
                client.DownloadFile(bulder.Uri, tempFileName);

                if (string.Equals(client.ResponseHeaders[HttpResponseHeader.ContentType], "application/gzip", StringComparison.OrdinalIgnoreCase))
                {
                    string newTempFileName = GetTempFileName(".tmp");
                  
                    using (var inStream = new GZipStream(File.OpenRead(tempFileName), CompressionMode.Decompress))
                    {
                        using (var outStream = File.Create(newTempFileName))
                        {
                            inStream.CopyTo(outStream);
                            outStream.Flush();
                            outStream.Close();
                        }
                        inStream.Close();
                    }

                    tempFileName.Delete(); // Delete zipped tmp.
                 
                    return newTempFileName;
                }
                    
                return tempFileName;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Download file to temp folder.
        /// </summary>
        /// <returns>The to temp with name.</returns>
        /// <param name="downloadUri">Download URI.</param>
        /// <param name="fileName">File name.</param>
        public LocalPath DownloadToTempWithName(string downloadUri, string fileName)
        {
            var path = DownloadToTemp(downloadUri);
          
            if (path.Exists)
            {
                var name = Path.GetFileName(fileName);
                var newName = path.GetDirectory() + name;
                return path.MoveTo(newName, true) ? newName : LocalPath.Empty();
            }

            return LocalPath.Empty();
        }

        public LocalPath Download(LocalPath path, string artifactUri)
        {
            var tempPath = DownloadToTemp(artifactUri);
          
            if (!tempPath.Exists || !tempPath.MoveTo(path, true))
                return LocalPath.Empty();
          
            return path;
        }
    }
}

