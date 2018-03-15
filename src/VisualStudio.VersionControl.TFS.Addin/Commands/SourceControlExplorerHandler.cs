using System.Linq;
using Autofac;
using MonoDevelop.Components.Commands;
using MonoDevelop.VersionControl.TFS.Gui.Views;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS.Commands
{
    public class SourceControlExplorerHandler : CommandHandler
    {
        protected override void Run()
        {
            var service = DependencyInjection.Container.Resolve<TeamFoundationServerVersionControlService>();
            var collection = service.Servers.SelectMany(x => x.ProjectCollections).First();
            var project = collection.Projects.FirstOrDefault();
         
            if (project != null)
            {
                SourceControlExplorerView.Show(project);
            }
        }

        protected override void Update(CommandInfo info)
        {
            var service = DependencyInjection.Container.Resolve<TeamFoundationServerVersionControlService>();

            var collectionsCount = service.Servers.SelectMany(x => x.ProjectCollections).Count();
            
            info.Visible = collectionsCount > 0;
        }
    }
}