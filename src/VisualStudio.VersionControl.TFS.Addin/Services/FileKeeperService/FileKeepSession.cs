// FileKeepSession.cs
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
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
    /// File keep session.
    /// </summary>
    public sealed class FileKeepSession : IFileKeepSession
	{
		bool _isDisposed;

        readonly ILoggingService _loggingService;
        readonly Dictionary<LocalPath, byte[]> _store = new Dictionary<LocalPath, byte[]>();

        public FileKeepSession(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public void Save(LocalPath[] localPaths, bool recursive)
        {
            var paths = recursive ? localPaths.SelectMany(p => p.CollectSubPathsAndSelf()) : localPaths;
          
            foreach (var path in paths)
            {
                _store.Add(path, path.IsDirectory ? null : File.ReadAllBytes(path));
            }
        }

        void Restore()
        {
            foreach (var map in _store)
            {
                try
                {
                    if (map.Value == null) //Directory
                    {
                        Directory.CreateDirectory(map.Key);
                    }
                    else
                    {
                        File.WriteAllBytes(map.Key, map.Value);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogToError(ex.Message);
                }
            }
            _store.Clear();
        }       

        public void Dispose()
        {
			if (_isDisposed)
                return;

            Restore();

			_isDisposed = true;
        }
    }
}