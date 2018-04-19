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
            ResolveConflictsView.Open(repository.Workspace, paths);
        }
    }
}