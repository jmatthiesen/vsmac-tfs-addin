using MonoDevelop.Components.Commands;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;

namespace MonoDevelop.VersionControl.TFS.Commands
{
    public class ConnectToServerHandler : CommandHandler
    {
        protected override void Run()
        {
            using (var dialog = new ConnectToServerDialog())
            {
                dialog.Run(Xwt.MessageDialog.RootWindow);
            }
        }

        protected override void Update(CommandInfo info)
        {  
            info.Enabled = true;

            base.Update(info);
        }
    }
}