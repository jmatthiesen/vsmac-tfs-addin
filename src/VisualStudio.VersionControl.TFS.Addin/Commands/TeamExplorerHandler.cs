using MonoDevelop.Components.Commands;

namespace VisualStudio.VersionControl.TFS.Addin.Commands
{
    public class TeamExplorerHandler : CommandHandler
    {
            protected override void Run ()
            {

            }

            protected override void Update (CommandInfo info)
            {
                info.Enabled = true;
            }   
    }
}