// TFSNodeExtension.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.VersionControl.TFS.Commands;
using MonoDevelop.VersionControl.TFS.Gui.Views;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Extensions
{
    public class TeamFoundationServerNodeExtension : NodeBuilderExtension
    {
        public override bool CanBuildNode(Type dataType)
        {
            return typeof(ProjectFile).IsAssignableFrom(dataType)
            || typeof(SystemFile).IsAssignableFrom(dataType)
            || typeof(IFolderItem).IsAssignableFrom(dataType)
            || typeof(WorkspaceObject).IsAssignableFrom(dataType);
        }

        public override Type CommandHandlerType
        {
            get { return typeof(TeamFoundationServerCommandHandler); }
        }
    }

    class TeamFoundationServerCommandHandler : VersionControlCommandHandler
    {
		/// <summary>
        /// Checkout a file from Solution pad.
        /// </summary>
        [CommandHandler(TeamExplorerCommands.Checkout)]
        protected void OnCheckoutFile()
        {
            foreach (var item in base.GetItems(false))
            {
                var repo = (TeamFoundationServerRepository)item.Repository;
                repo.CheckoutFile(new LocalPath(item.Path));
            }
        }

        [CommandUpdateHandler(TeamExplorerCommands.Checkout)]
        protected void UpdateCheckoutFile(CommandInfo commandInfo)
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                commandInfo.Visible = false;
                return;
            }

            foreach (var item in GetItems(false))
            {
                if (item.IsDirectory)
                {
                    commandInfo.Visible = false;
                    return;
                }               

				if (!(item.Repository is TeamFoundationServerRepository repo))
				{
					commandInfo.Visible = false;
					return;
				}

				if (!item.VersionInfo.IsVersioned || item.VersionInfo.HasLocalChanges || item.VersionInfo.Status.HasFlag(VersionStatus.Locked))
                {
                    commandInfo.Visible = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Resolve merge conflicts.
        /// </summary>
        [CommandHandler(TeamExplorerCommands.ResolveConflicts)]
        protected void OnResolveConflicts()
        {
            var items = base.GetItems(false);
          
            if (items.Count == 0)
                return;
            
            var item = items[0];
            var repo = (TeamFoundationServerRepository)item.Repository;

            ResolveConflictsView.Open(repo.Workspace, GetWorkingPaths(item));
        }

        List<LocalPath> GetWorkingPaths(VersionControlItem item)
        {
            var paths = new List<LocalPath>();

			if (item.WorkspaceObject is Solution solution)
			{
				//Add Solution
				paths.Add(new LocalPath(solution.BaseDirectory));

				//Add linked files.
				foreach (var path in solution.GetItemFiles(true))
				{
					if (!path.IsChildPathOf(solution.BaseDirectory))
					{
						paths.Add(new LocalPath(path));
					}
				}
			}
			else
			{
				var project = (Project)item.WorkspaceObject;
				paths.Add(new LocalPath(project.BaseDirectory));
			}

			return paths;
        }

        [CommandUpdateHandler(TeamExplorerCommands.ResolveConflicts)]
        protected void UpdateResolveConflicts(CommandInfo commandInfo)
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                commandInfo.Visible = false;
                return;
            }

            var items = base.GetItems(false);

            if (items.Count == 0)
            {
                commandInfo.Visible = false;
                return;
            }

            var item = items[0];

			if (!(item.Repository is TeamFoundationServerRepository repo) || !item.VersionInfo.IsVersioned)
			{
				commandInfo.Visible = false;
				return;
			}

			commandInfo.Visible = false; // Functionality not available for now
        }

        /// <summary>
		/// Open the specific file in the source explorer view.
        /// </summary>
        [CommandHandler(TeamExplorerCommands.LocateInSourceExplorer)]
        protected void OnLocateInSourceExplorer()
        {
            var item = base.GetItems(false)[0];
            var repo = (TeamFoundationServerRepository)item.Repository;
            var path = item.Path;
            string fileName = null;
           
            if (!item.IsDirectory)
            {
                fileName = path.FileName;
                path = path.ParentDirectory;
            }

            var serverPath = repo.Workspace.Data.GetServerPathForLocalPath(new LocalPath(path));
            SourceControlExplorerView.Show(repo.Workspace.ProjectCollection, serverPath, fileName);
        }
        
        [CommandUpdateHandler(TeamExplorerCommands.LocateInSourceExplorer)]
        protected void UpdateLocateInSourceExplorer(CommandInfo commandInfo)
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                commandInfo.Visible = false;
                return;
            }

            var items = base.GetItems(false);

            if (items.Count != 1)
            {
                commandInfo.Visible = false;
                return;
            }

            foreach (var item in items)
            {
				if (!(item.Repository is TeamFoundationServerRepository repo))
				{
					commandInfo.Visible = false;
					return;
				}

				if (!item.VersionInfo.IsVersioned)
                {
                    commandInfo.Visible = false;
                    return;
                }
            }
        }
    }
}

