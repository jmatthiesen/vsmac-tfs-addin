using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;

namespace MonoDevelop.VersionControl.TFS.Commands
{
    public class ConnectToServerHandler : CommandHandler
    {
        protected override void Run()
        {
            var RootWindow = IdeApp.Workbench.RootWindow;
            Xwt.MessageDialog.RootWindow = Xwt.Toolkit.CurrentEngine.WrapWindow(IdeApp.Workbench.RootWindow);

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