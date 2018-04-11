using Autofac;
using MonoDevelop.Components.Commands;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS.Commands
{
    public class CheckoutHandler : CommandHandler
    {
        protected override void Run()
        {
            var service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            using (var checkOutMapDialog = new CheckOutMapDialog(service))
            {
                checkOutMapDialog.Run(Xwt.MessageDialog.RootWindow);
            }
        }

        protected override void Update(CommandInfo info)
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                info.Enabled = false;
                return;
            }

            var service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            var serversCount = service.Servers.Count;

            info.Visible = serversCount > 0;
        }
    }
}