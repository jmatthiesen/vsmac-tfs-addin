using System;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.VersionControl.TFS.Commands;

namespace MonoDevelop.VersionControl.TFS.Extensions
{
    public class TeamFoundationServerNodeExtension : NodeBuilderExtension
    {
        public override bool CanBuildNode(Type dataType)
        {
            return typeof(ProjectFile).IsAssignableFrom(dataType)
            || typeof(SystemFile).IsAssignableFrom(dataType)
            || typeof(ProjectFolder).IsAssignableFrom(dataType)
            || typeof(WorkspaceObject).IsAssignableFrom(dataType);
        }

        public override Type CommandHandlerType
        {
            get { return typeof(TFSCommandHandler); }
        }
    }

    public class TFSCommandHandler : VersionControlCommandHandler 
    {
        [CommandHandler(TeamExplorerCommands.Checkout)]
        protected void OnCheckoutFile()
        {
            foreach (var item in GetItems(false))
            {
                var repo = (TeamFoundationServerRepository)item.Repository;
                repo.CheckoutFile(item.Path);      
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

                var repo = item.Repository as TeamFoundationServerRepository;
              
                if (repo == null)
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

        [CommandHandler(TeamExplorerCommands.LocateInSourceExplorer)]
        protected void OnLocateInSourceExplorer()
        {
            var item = GetItems(false)[0];
            var repo = (TeamFoundationServerRepository)item.Repository;
            var path = item.Path;
            string fileName = null;
           
            if (!item.IsDirectory)
            {
                fileName = path.FileName;
                path = path.ParentDirectory;
            }

            var workspace = repo.GetWorkspaceByLocalPath(path);

            if (workspace == null)
                return;
            
            var serverPath = workspace.GetServerPathForLocalPath(path);
            SourceControlExplorerView.Show(workspace.ProjectCollection, serverPath, fileName);
        }

        [CommandUpdateHandler(TeamExplorerCommands.LocateInSourceExplorer)]
        protected void UpdateLocateInSourceExplorer(CommandInfo commandInfo)
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                commandInfo.Visible = false;
                return;
            }

            var items = GetItems(false);
         
            if (items.Count != 1)
            {
                commandInfo.Visible = false;
                return;
            }

            foreach (var item in items)
            {
                var repo = item.Repository as TeamFoundationServerRepository;
               
                if (repo == null)
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

        [CommandHandler(TeamExplorerCommands.ResolveConflicts)]
        protected void OnResolveConflicts()
        {
            var items = GetItems(false);

            if (items.Count == 0)
                return;
            
            var item = items[0];
            var repo = (TeamFoundationServerRepository)item.Repository; 
            ResolveConflictsView.Open(repo, GetWorkingPaths(item));
        }

        [CommandUpdateHandler(TeamExplorerCommands.ResolveConflicts)]
        protected void UpdateResolveConflicts(CommandInfo commandInfo)
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                commandInfo.Visible = false;
                return;
            }

            var items = GetItems(false);
           
            if (items.Count == 0)
            {
                commandInfo.Visible = false;
                return;
            }

            var item = items[0];
            var repo = item.Repository as TeamFoundationServerRepository;
           
            if (repo == null)
            {
                commandInfo.Visible = false;
                return;
            }

            commandInfo.Visible = true;
        }

        List<FilePath> GetWorkingPaths(VersionControlItem item)
        {
            List<FilePath> paths = new List<FilePath>();
            var solution = item.WorkspaceObject as Solution;
          
            if (solution != null)
            {
                paths.Add(solution.BaseDirectory);
                foreach (var path in solution.GetItemFiles(true))
                {
                    if (!path.IsChildPathOf(solution.BaseDirectory))
                    {
                        paths.Add(path);
                    }
                }
            }
            else
            {
                var project = (Project)item.WorkspaceObject;
                paths.Add(project.BaseDirectory);
            }

            return paths;
        }
    }
}