// NotificationService.cs
// 
// Author:
//       Ventsislav Mladenov
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2015 Ventsislav Mladenov
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.MonoDevelopWrappers.Implementation
{
	/// <summary>
    /// Notification service.
    /// </summary>
    internal sealed class NotificationService : INotificationService
    {
        readonly TeamFoundationServerRepository _repository;

        public NotificationService(TeamFoundationServerRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Notifies the file changed.
        /// </summary>
        /// <param name="path">Path.</param>
        public void NotifyFileChanged(string path)
        {
			var filePath = new FilePath(path);
			VersionControlService.NotifyFileStatusChanged(new FileUpdateEventArgs(_repository, filePath, filePath.IsDirectory));
			FileService.NotifyFileChanged(filePath);
        }

        public void NotifyFilesChanged(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                NotifyFileChanged(path);
            }
        }

        public void NotifyFilesChanged(IEnumerable<LocalPath> paths)
        {
            NotifyFilesChanged(paths.Select(x => (string)x));
        }

        /// <summary>
        /// Notifies the file removed.
        /// </summary>
        /// <param name="path">Path.</param>
        public void NotifyFileRemoved(string path)
        {
            var filePath = new FilePath(path);
			VersionControlService.NotifyFileStatusChanged(new FileUpdateEventArgs(_repository, filePath, filePath.IsDirectory));
			FileService.NotifyFileRemoved(filePath);
        }

        public void NotifyFilesRemoved(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                NotifyFileRemoved(path);
            }
        }

        public void NotifyFilesRemoved(IEnumerable<LocalPath> paths)
        {
            NotifyFilesRemoved(paths.Select(x => (string)x));
        }
    }
}