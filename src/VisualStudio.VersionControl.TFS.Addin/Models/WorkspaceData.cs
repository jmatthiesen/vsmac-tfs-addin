// WorkspaceData.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Helpers;

namespace MonoDevelop.VersionControl.TFS.Models
{
    public sealed class WorkspaceData
    {
        public WorkspaceData()
        {
            WorkingFolders = new List<WorkingFolder>();
        }

        public string Name { get; set; }

        public string Owner { get; set; }

        public string Computer { get; set; }

        public string Comment { get; set; }

        public bool IsLocal { get; set; }

        public List<WorkingFolder> WorkingFolders { get; set; }

        #region Working Folders

        public bool IsLocalPathMapped(LocalPath localPath)
        {
            if (localPath == null)
				throw new ArgumentNullException(nameof(localPath));
          
			var isLocalPathMapped = WorkingFolders.Any(f => localPath.IsChildOrEqualOf(f.LocalItem));

			return isLocalPathMapped;	
		}

        public bool IsServerPathMapped(RepositoryPath serverPath)
        {
            return WorkingFolders.Any(f => serverPath.IsChildOrEqualOf(f.ServerItem));
        }

        public RepositoryPath GetServerPathForLocalPath(LocalPath localPath)
        {
            var mappedFolder = WorkingFolders.FirstOrDefault(f => localPath.IsChildOrEqualOf(f.LocalItem));
          
            if (mappedFolder == null)
                return null;
           
            if (localPath == mappedFolder.LocalItem)
                return mappedFolder.ServerItem;
           
            var relativePath = localPath.ToRelativeOf(mappedFolder.LocalItem);
           
            if (!EnvironmentHelper.IsRunningOnUnix)
                relativePath = relativePath.Replace(Path.DirectorySeparatorChar, RepositoryPath.Separator);
           
            return new RepositoryPath(mappedFolder.ServerItem + relativePath, localPath.IsDirectory);
        }

        public LocalPath GetLocalPathForServerPath(RepositoryPath serverPath)
        {
            var mappedFolder = WorkingFolders.FirstOrDefault(f => serverPath.IsChildOrEqualOf(f.ServerItem));
          
            if (mappedFolder == null)
                return null;
           
            if (serverPath == mappedFolder.ServerItem)
                return mappedFolder.LocalItem;
            else
            {
                string relativePath = serverPath.ToRelativeOf(mappedFolder.ServerItem);
                if (!EnvironmentHelper.IsRunningOnUnix)
                {
                    relativePath = relativePath.Replace(RepositoryPath.Separator, Path.DirectorySeparatorChar);
                }

                return Path.Combine(mappedFolder.LocalItem, relativePath);
            }
        }

        public WorkingFolder GetWorkingFolderForServerItem(RepositoryPath serverPath)
        {
            return WorkingFolders.SingleOrDefault(wf => serverPath.IsChildOrEqualOf(wf.ServerItem));
        }

        #endregion

        #region Serialization

        public static WorkspaceData FromServerXml(XElement element)
        {
            var data = new WorkspaceData();
            data.Name = element.GetAttributeValue("name");
            data.Owner = element.GetAttributeValue("owner");
            data.Computer = element.GetAttributeValue("computer");
            data.IsLocal = element.GetBooleanAttribute("islocal");
            data.Comment = element.GetElement("Comment").Value;
            data.WorkingFolders.AddRange(element.GetDescendants("WorkingFolder").Select(WorkingFolder.FromXml));
          
            return data;
        }

        internal XElement ToXml(string elementName)
        {
            XElement element = new XElement(elementName, 
                new XAttribute("computer", Computer), 
                new XAttribute("name", Name),
                new XAttribute("owner", Owner), 
                new XElement("Comment", Comment)
            );

            element.Add(new XElement("Folders", WorkingFolders.Select(f => f.ToXml())));
         
            return element;
        }

        #endregion
    }
}