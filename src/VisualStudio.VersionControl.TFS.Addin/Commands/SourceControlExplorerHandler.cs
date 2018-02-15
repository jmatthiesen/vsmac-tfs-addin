using System.Linq;
using MonoDevelop.Components.Commands;
using VisualStudio.VersionControl.TFS.Addin.Gui.Views;

namespace VisualStudio.VersionControl.TFS.Addin.Commands
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