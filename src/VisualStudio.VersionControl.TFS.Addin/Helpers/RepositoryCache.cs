//
// RepositoryCache.cs
//
// Author:
//       Ventsislav Mladenov <vmladenov.mladenov@gmail.com>
//       Javier Suárez Ruiz  <javiersuarezruiz@hotmail.com>
//
// Copyright (c) 2018 Ventsislav Mladenov, Javier Suárez Ruiz
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
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.TFS.Helpers
{
    public class RepositoryCache
    {
        readonly TeamFoundationServerRepository _repo;
        readonly List<ExtendedItem> _cachedItems;
        static readonly object _locker = new object();

        public RepositoryCache(TeamFoundationServerRepository repo)
        {
            _repo = repo;
            _cachedItems = new List<ExtendedItem>();
        }

        public void AddToCache(IEnumerable<ExtendedItem> items)
        {
            foreach (var item in items)
            {
                AddToCache(item);
            }
        }

        public void AddToCache(ExtendedItem item)
        {
            if (item == null)
                return;
            
            if (_cachedItems.Contains(item))
                _cachedItems.Remove(item);
            
            _cachedItems.Add(item);
        }

        public void ClearCache()
        {
            _cachedItems.Clear();
        }

        public bool HasItem(VersionControlPath serverPath)
        {
            return _cachedItems.Any(c => c.ServerPath == serverPath);
        }

        public ExtendedItem GetItem(VersionControlPath serverPath)
        {
            return _cachedItems.Single(c => c.ServerPath == serverPath);
        }

        public ExtendedItem GetItem(FilePath localPath)
        {
            lock (_locker)
            {
                var workspace = _repo.GetWorkspaceByLocalPath(localPath);

                if (workspace == null)
                    return null;
                
                var serverPath = workspace.GetServerPathForLocalPath(localPath);
                var item = _cachedItems.SingleOrDefault(ex => ex.ServerPath == serverPath);
              
                if (item == null)
                {
                    var repoItem = workspace.GetExtendedItem(serverPath, ItemType.Any);
                    AddToCache(repoItem);
                    return repoItem;
                }
                else
                    return item;
            }
        }

        public List<ExtendedItem> GetItems(List<FilePath> paths, RecursionType recursionType)
        {
            lock(_locker)
            {
                List<ExtendedItem> items = new List<ExtendedItem>();
                var workspaceFilesMapping = new Dictionary<Workspace, List<ItemSpec>>();
              
                foreach (var path in paths) 
                {
                    var workspace = _repo.GetWorkspaceByLocalPath(path);
                 
                    if (workspace == null)
                        continue;
                    
                    var serverPath = workspace.GetServerPathForLocalPath(path);
                    if (HasItem(serverPath) && recursionType == RecursionType.None)
                    {
                        items.Add(GetItem(serverPath));
                        continue;
                    }
                    if (workspaceFilesMapping.ContainsKey(workspace))
                        workspaceFilesMapping[workspace].Add(new ItemSpec(serverPath, recursionType));
                    else
                        workspaceFilesMapping.Add(workspace, new List<ItemSpec> { new ItemSpec(serverPath, recursionType) });
                }

                foreach (var workspaceMap in workspaceFilesMapping)
                {
                    var extendedItems = workspaceMap.Key.GetExtendedItems(workspaceMap.Value, DeletedState.NonDeleted, ItemType.Any).Distinct();
                    AddToCache(extendedItems);
                    items.AddRange(extendedItems);
                }

                return items;
            }
        }

        public void RefreshItems(IEnumerable<FilePath> localPaths)
        {
            foreach (var path in localPaths)
            {
                RefreshItem(path);
            }
        }

        public void RefreshItem(FilePath localPath)
        {
            lock(_locker)
            {
                var workspace = _repo.GetWorkspaceByLocalPath(localPath);
             
                if (workspace == null)
                    return;
                
                var serverPath = workspace.GetServerPathForLocalPath(localPath);
                var repoItem = workspace.GetExtendedItem(serverPath, ItemType.Any);
                AddToCache(repoItem);
            }
        }
    }
}