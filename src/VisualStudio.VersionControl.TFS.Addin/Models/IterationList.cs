// IterationList.cs
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

namespace MonoDevelop.VersionControl.TFS.Models
{
    sealed class IterationList : List<IIteration>
    {
        const int IterationParentType = -43;
        readonly List<WorkItemProject> projects;

        public IterationList(List<WorkItemProject> projects)
        {
            this.projects = projects;
        }

        public void Build(List<Hierarchy> hierarchy)
        {
            var projectIterationsNodes = hierarchy.Where(h => !h.IsDeleted && h.TypeId == IterationParentType).ToArray();
          
            foreach (var projectIterationNode in projectIterationsNodes)
            {
                var projectIterationNode1 = projectIterationNode;
                var mainIterationNodes = hierarchy.Where(h => !h.IsDeleted && h.ParentId == projectIterationNode1.AreaId).ToArray();
           
                foreach (var mainIterationNode in mainIterationNodes)
                {
                    var mainIteration = new MainIteration();
                    mainIteration.Id = mainIterationNode.AreaId;
                    mainIteration.Name = mainIterationNode.Name;
                    mainIteration.Project = projects.Single(p => p.Id == projectIterationNode1.ParentId);
                    Add(mainIteration);
                    BuildSubIterations(hierarchy, mainIteration);
                }
            }
        }

        void BuildSubIterations(List<Hierarchy> hierarchy, IIteration iteration)
        {
            foreach (var subIterationNode in hierarchy.Where(h => !h.IsDeleted && h.ParentId == iteration.Id))
            {
                var subIteration = new SubIteration();
                subIteration.Id = subIterationNode.AreaId;
                subIteration.Name = subIterationNode.Name;
                iteration.Children.Add(subIteration);
                Add(subIteration);
                BuildSubIterations(hierarchy, subIteration);
            }
        }

        public IIteration LocateIteration(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            
            var pathParts = path.Split(new [] { '\\' }, System.StringSplitOptions.RemoveEmptyEntries);
          
            if (pathParts.Length < 2)
                return null;
            
            var projectName = pathParts[0];
            var iterationName = pathParts[1];
            var mainIteration = this.OfType<MainIteration>().Single(i => string.Equals(i.Project.Name, projectName, System.StringComparison.OrdinalIgnoreCase) && 
                                                                         string.Equals(i.Name, iterationName, System.StringComparison.OrdinalIgnoreCase));
            if (pathParts.Length == 2) //Only 1 level
                return mainIteration;
            else
            {
                List<SubIteration> iterations = mainIteration.Children;
                for (int i = 2; i < pathParts.Length; i++)
                {
                    var currentPart = pathParts[i];

                    foreach (var iteration in iterations)
                    {
                        if (string.Equals(iteration.Name, currentPart, System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (i < pathParts.Length -1)
                            {
                                iterations = iteration.Children;
                                break;
                            }
                            else
                                return iteration;
                        }
                    }
                }

                return null;
            }
        }
    }
}