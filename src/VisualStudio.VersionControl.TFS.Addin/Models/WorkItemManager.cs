// WorkItemManager.cs
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
using System.Text;

namespace MonoDevelop.VersionControl.TFS.Models
{
    public sealed class WorkItemManager
    {
        readonly ProjectCollection collection;

        public WorkItemManager(ProjectCollection collection)
        {
            this.collection = collection;
            Init();
        }

        void Init()
        {
            CachedMetaData.Instance.Init(collection);
            var constants = CachedMetaData.Instance.Constants;
            var userNameBuilder = new StringBuilder();

            userNameBuilder.Append(collection.Server.UserName);
            var userName = userNameBuilder.ToString();
            var me = constants.FirstOrDefault(c => string.Equals(c.Value, userName, StringComparison.OrdinalIgnoreCase));
           
            if (me != null)
            {
                WorkItemsContext.WhoAmI = me.DisplayName;
                WorkItemsContext.MySID = me.SID;
            }
        }

        public WorkItemProject GetByGuid(Guid guid)
        {
            return CachedMetaData.Instance.Projects.SingleOrDefault(p => p.Guid == guid);
        }

        public List<StoredQuery> GetPublicQueries(WorkItemProject project)
        {
            return collection.GetStoredQueries(project).Where(q => q.IsPublic && !q.IsDeleted).OrderBy(q => q.QueryName).ToList();
        }

        public List<StoredQuery> GetMyQueries(WorkItemProject project)
        {
            return collection.GetStoredQueries(project).Where(q => string.Equals(WorkItemsContext.MySID, q.Owner) && !q.IsDeleted).ToList();
        }

        public void UpdateWorkItems(int changeSet, Dictionary<int, WorkItemCheckinAction> workItems, string comment)
        {
            if (workItems == null)
                return;
            
            foreach (var workItem in workItems)
            {
                switch (workItem.Value)
                {
                    case WorkItemCheckinAction.Associate:
                        collection.AssociateWorkItemWithChangeset(workItem.Key, changeSet, comment);
                        break;
                    case WorkItemCheckinAction.Resolve:
                        collection.ResolveWorkItemWithChangeset(workItem.Key, changeSet, comment);
                        break;
                }
            }
        }
    }
}