// ResolveConflictsHandler.cs
// 
// Author:
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2018 Javier Suárez Ruiz
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Gui.Views;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Commands
{
    public class ResolveConflictsHandler : CommandHandler
    {
        protected override void Update(CommandInfo info)
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                info.Visible = false;
                return;
            }

            var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
           
            if (solution == null)
            {
                info.Visible = false;
                return;
            }

			info.Visible = false; // Functionality not available for now
        }

        protected override void Run()
        {
            var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
            List<LocalPath> paths = new List<LocalPath>();
           
            // Solution
            paths.Add(new LocalPath(solution.BaseDirectory));
      
            // Files
            foreach (var path in solution.GetItemFiles(true))
            {
                if (!path.IsChildPathOf(solution.BaseDirectory))
                {
                    paths.Add(new LocalPath(path));
                }
            }

            var repository = (TeamFoundationServerRepository)VersionControlService.GetRepository(solution);
            
            // Open ResolveConflictsView
			ResolveConflictsView.Open(repository.Workspace, paths);
        }
    }
}