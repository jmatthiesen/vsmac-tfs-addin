// WorkingFolder.cs
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
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MonoDevelop.VersionControl.TFS.Models
{
    [XmlType(Namespace = "http://schemas.microsoft.com/TeamFoundation/2005/06/VersionControl/ClientServices/03")]
    public sealed class WorkingFolder
    {
        public WorkingFolder(string serverItem, string localItem)
        {
            CheckServerPathStartsWithDollarSlash(serverItem);
            ServerItem = new RepositoryPath(serverItem, true);
            LocalItem = Path.GetFullPath(localItem);
            Type = WorkingFolderType.Map;
        }

        internal static WorkingFolder FromXml(XElement element)
        {
            var local = new LocalPath(element.Attribute("local").Value);
            string serverItem = new RepositoryPath(element.Attribute("item").Value, true);
            var workFolder = new WorkingFolder(serverItem, local);
          
            if (element.Attribute("type") != null)
                workFolder.Type = (WorkingFolderType)Enum.Parse(typeof(WorkingFolderType), element.Attribute("type").Value);
           
            return workFolder;
        }

        internal XElement ToXml()
        {
            return new XElement("WorkingFolder", 
                new XAttribute("local", LocalItem.ToRepositoryLocalPath()), 
                new XAttribute("item", ServerItem),
                new XAttribute("type", Type.ToString())
            );
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("WorkingFolder instance ");
            sb.Append(GetHashCode());

            sb.Append("\n	 LocalItem: ");
            sb.Append(LocalItem);

            sb.Append("\n	 ServerItem: ");
            sb.Append(ServerItem);

            return sb.ToString();
        }

        internal void CheckServerPathStartsWithDollarSlash(string serverItem)
        {
            if (RepositoryPath.IsServerItem(serverItem))
                return;
            
            string msg = String.Format("TF10125: The path '{0}' must start with {1}", serverItem, RepositoryPath.RootPath);
            throw new VersionControlException(msg);
        }

        public bool IsCloaked { get { return Type == WorkingFolderType.Cloak; } }

        public LocalPath LocalItem { get; private set; }

        public WorkingFolderType Type { get; private set; }

        public RepositoryPath ServerItem { get; private set; }
    }
}