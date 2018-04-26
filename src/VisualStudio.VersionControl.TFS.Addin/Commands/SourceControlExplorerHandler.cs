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
            var service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

			if (service.Servers.Count == 1)
			{
				var collection = service.Servers.SelectMany(x => x.ProjectCollections).First();
				var project = collection.Projects.FirstOrDefault();
                
				if (project != null)
				{
					SourceControlExplorerView.Show(project);
				}
			}
			else
			{
				var servers = service.Servers;
				SourceControlExplorerView.Show(servers);
			}
        }

        protected override void Update(CommandInfo info)
        {
            var service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            var collectionsCount = service.Servers.SelectMany(x => x.ProjectCollections).Count();
            
            info.Visible = collectionsCount > 0;
        }
    }
}