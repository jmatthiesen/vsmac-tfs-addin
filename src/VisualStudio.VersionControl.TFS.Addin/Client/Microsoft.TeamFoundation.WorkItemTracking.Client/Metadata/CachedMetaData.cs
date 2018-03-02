//
// CachedMetaData.cs
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
using Microsoft.TeamFoundation.WorkItemTracking.Client.Objects;

namespace Microsoft.TeamFoundation.WorkItemTracking.Client.Metadata
{
    public class CachedMetaData
    {
        static CachedMetaData instance;

        public static CachedMetaData Instance
        {
            get
            {
                return instance ?? (instance = new CachedMetaData());
            }
        }

        public void Init(ClientService clientService)
        {
            var hierarchy = clientService.GetHierarchy();
            ExtractProjects(hierarchy);
            Fields = new FieldList(clientService.GetFields());
            Constants = clientService.GetConstants();
            ExtractWorkItemTypes(clientService);
            ExtractActions(clientService);
        }

        void ExtractProjects(List<Hierarchy> hierarchy)
        {
            const int projectType = -42;

            Projects = new List<Project>();

            foreach (var item in hierarchy.Where(h => h.TypeId == projectType && !h.IsDeleted))
            {
                Projects.Add(new Project { Id = item.AreaId, Guid = item.Guid, Name = item.Name });
            }

            Iterations = new IterationList(Projects);
            Iterations.Build(hierarchy);
        }

        void ExtractWorkItemTypes(ClientService clientService)
        {
            var types = clientService.GetWorkItemTypes();

            foreach (var type in types)
            {
                var type1 = type;
                type.Project = Projects.Single(p => p.Id == type1.ProjectId);
                type.Name = Constants.Single(c => c.Id == type1.NameConstantId);
            }

            WorkItemTypes = types;
        }

        void ExtractActions(ClientService clientService)
        {
            var actions = clientService.GetActions();
         
            foreach (var action in actions)
            {
                var action1 = action;
                action.WorkItemType = WorkItemTypes.Single(t => t.Id == action1.WorkItemTypeId);
                action.FromState = Constants.Single(c => c.Id == action1.FromStateId);
                action.ToState = Constants.Single(c => c.Id == action1.ToStateId);
            }

            Actions = actions;
        }

        public FieldList Fields { get; set; }

        public List<Constant> Constants { get; set; }

        public List<Project> Projects { get; set; }

        internal IterationList Iterations { get; set; }

        public List<WorkItemType> WorkItemTypes { get; set; }

        public List<Action> Actions { get; set; }
    }
}