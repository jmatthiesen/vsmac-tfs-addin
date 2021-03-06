﻿// RepositoryCache.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS.Helpers
{
    internal class RepositoryCache
    {
        readonly TeamFoundationServerRepository repo;
        readonly List<ExtendedItem> cachedItems;
        static readonly object locker = new object();

        public RepositoryCache(TeamFoundationServerRepository repo)
        {
            this.repo = repo;
            cachedItems = new List<ExtendedItem>();
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

            if (cachedItems.Contains(item))
                cachedItems.Remove(item);

            cachedItems.Add(item);
        }

        public void ClearCache()
        {
            cachedItems.Clear();
        }

        bool HasItem(RepositoryPath serverPath)
        {
            return cachedItems.Any(c => c.ServerPath == serverPath);
        }

        ExtendedItem GetItem(RepositoryPath serverPath)
        {
            return cachedItems.Single(c => c.ServerPath == serverPath);
        }

        public List<ExtendedItem> GetItems(List<LocalPath> paths, RecursionType recursionType)
        {
            lock (locker)
            {
                List<ExtendedItem> items = new List<ExtendedItem>();
                var workspaceFilesMapping = new Dictionary<IWorkspaceService, List<ItemSpec>>();

                foreach (var path in paths)
                {
                    var workspace = repo.Workspace;

                    if (workspace == null)
                        continue;

                    var serverPath = workspace.Data.GetServerPathForLocalPath(path);

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

        public void RefreshItems(IEnumerable<LocalPath> localPaths)
        {
            foreach (var path in localPaths)
            {
                RefreshItem(path);
            }
        }

        public void RefreshItem(LocalPath localPath)
        {
            lock (locker)
            {
                var workspace = repo.Workspace;

                if (workspace == null)
                    return;

                var repoItem = workspace.GetExtendedItem(ItemSpec.FromLocalPath(localPath), ItemType.Any);
                AddToCache(repoItem);
            }
        }
    }
}