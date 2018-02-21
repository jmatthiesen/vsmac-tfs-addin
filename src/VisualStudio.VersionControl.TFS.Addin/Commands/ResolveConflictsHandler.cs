using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Gui.Views;

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

            var repo = VersionControlService.GetRepository(solution) as TeamFoundationServerRepository;
          
            if (repo == null)
            {
                info.Visible = false;
                return;
            }
        }

        protected override void Run()
        {
            var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
            List<FilePath> paths = new List<FilePath>();
            paths.Add(solution.BaseDirectory);
      
            foreach (var path in solution.GetItemFiles(true))
            {
                if (!path.IsChildPathOf(solution.BaseDirectory))
                {
                    paths.Add(path);
                }
            }

            var repository = (TeamFoundationServerRepository)VersionControlService.GetRepository(solution);
            ResolveConflictsView.Open(repository, paths);
        }
    }
}

