using MonoDevelop.Components.Commands;
using VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs;

namespace VisualStudio.VersionControl.TFS.Addin.Commands
{
    public class ConnectToServerHandler : CommandHandler
    {
        protected override void Run()
        {
            var dialog = new ConnectToServerDialog();
            dialog.Show();
        
            /*
            using (var dialog = new ConnectToServerDialog())
            {
                dialog.Run(Toolkit.CurrentEngine.WrapWindow(MessageService.RootWindow));
            }
            */
        }

        protected override void Update(CommandInfo info)
        {  
            info.Enabled = true;

            base.Update(info);
        }
    }
}