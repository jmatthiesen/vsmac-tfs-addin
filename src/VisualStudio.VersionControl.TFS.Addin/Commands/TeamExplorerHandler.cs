using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl.TFS.Commands
{
    public class TeamExplorerHandler : CommandHandler
    {
        protected override void Run ()
        {
            Pad pad = null;

            var pads = IdeApp.Workbench.Pads; 
 
            foreach (var p in IdeApp.Workbench.Pads)
            {
                if (string.Equals(p.Id, "MonoDevelop.MonoDevelop.TFS.TeamExplorerPad", System.StringComparison.OrdinalIgnoreCase))
                {
                    pad = p;
                }
            }

            if (pad == null)
            {
                var content = new Gui.Pads.TeamExplorerPad();

                pad = IdeApp.Workbench.ShowPad(content, "MonoDevelop.MonoDevelop.TFS.TeamExplorerPad", "Team Explorer", "Right", null);
               
                if (pad == null)
                    return;
            }

            pad.Sticky = true;
            pad.AutoHide = false;
            pad.BringToFront();
        }

        protected override void Update (CommandInfo info)
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                info.Enabled = false;
                return;
            }

            var serversCount = TeamFoundationServerClient.Settings.GetServers().Count();

            info.Visible = serversCount > 0;
        }   
    }
}