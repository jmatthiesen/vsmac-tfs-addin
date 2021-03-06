// WorkItemStore.cs
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
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.TFS.Models
{
    sealed class WorkItemStore
    {
        readonly StoredQuery query;
        readonly ProjectCollection collection;

        public WorkItemStore(StoredQuery query, ProjectCollection collection)
        {
            this.query = query;
            this.collection = collection;
        }

        public List<WorkItem> LoadByWorkItem(ProgressMonitor progress)
        {
            var ids = collection.GetWorkItemIds(query, CachedMetaData.Instance.Fields);
            var list = new List<WorkItem>();
            progress.BeginTask("Loading WorkItems", ids.Count);

            foreach (var id in ids)
            {
                list.Add(collection.GetWorkItem(id));
                progress.Step(1);
            }

            progress.EndTask();

            return list;
        }

        public List<WorkItem> LoadByPage(ProgressMonitor progress)
		{
			try
			{
				var ids = collection.GetWorkItemIds(query, CachedMetaData.Instance.Fields);

				if (!ids.Any())
				{
					ids = collection.GetWorkItemIds(query, CachedMetaData.Instance.Projects.FirstOrDefault(p => p.Id == query.ProjectId));
				}

				int pages = (int)Math.Ceiling((double)ids.Count / (double)50);
				var result = new List<WorkItem>();
				progress.BeginTask(GettextCatalog.GetString("Loading WorkItems"), pages);

				for (int i = 0; i < pages; i++)
				{
					var idList = new List<int>(ids.Skip(i * 50).Take(50));
					var items = collection.PageWorkitemsByIds(query, idList);
					result.AddRange(items);
					progress.Step();
				}

				progress.EndTask();

				return result;
			}
			catch
			{
				return new List<WorkItem>();
			}
        }
    }
}