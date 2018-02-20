//
// TfsRevision.cs
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

using System;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;

namespace MonoDevelop.VersionControl.TFS.Models
{
    public class TeamFoundationServerRevision : Revision
    {
        public int Version { get; set; }

        public string ItemPath { get; set; }

        public TeamFoundationServerRevision(Repository repo, int version, string itemPath) : base(repo)
        {
            Version = version;
            ItemPath = itemPath;
        }

        public TeamFoundationServerRevision(Repository repo, string itemPath, Changeset changeset) :
            this(repo, changeset.ChangesetId, itemPath)
        {
            Author = changeset.Committer;
            Message = changeset.Comment;
            Time = changeset.CreationDate;
        }

        public void Load(RepositoryService service)
        {
            var changeset = service.QueryChangeset(this.Version);
            Author = changeset.Committer;
            Message = changeset.Comment;
            Time = changeset.CreationDate;
        }

        public override Revision GetPrevious()
        {
            if (Version <= 0)
                return null;
            
            var repo = (TeamFoundationServerRepository)this.Repository;
           
            var changeSets = repo.VersionControlService.QueryHistory(
                new ItemSpec(ItemPath, RecursionType.None),   
                new ChangesetVersionSpec(Version), null,           
                new ChangesetVersionSpec(Version), 2);
            
            if (changeSets.Count == 2)
            {
                return new TeamFoundationServerRevision(repo, ItemPath, changeSets[1]);
            }

            return null;
        }

        public override string ToString()
        {
            return Convert.ToString(Version);
        }
    }
}