using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs;

namespace VisualStudio.VersionControl.TFS.Addin.Commands
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