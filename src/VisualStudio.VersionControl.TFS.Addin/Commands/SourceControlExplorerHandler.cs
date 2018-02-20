using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.VersionControl.TFS.Gui.Views;

namespace MonoDevelop.VersionControl.TFS.Commands
{
    public class SourceControlExplorerHandler : CommandHandler
    {
        protected override void Run()
        {
            var projectCollection = TeamFoundationServerClient.Settings.GetServers()
                                      .SelectMany(x => x.ProjectCollections).Single();
            SourceControlExplorerView.Show(projectCollection);
        }

        protected override void Update(CommandInfo info)
        {
            var collectionsCount = TeamFoundationServerClient.Settings.GetServers()
                                                              .SelectMany(x => x.ProjectCollections).Count();

            info.Visible = collectionsCount > 0;
        }
    }
}