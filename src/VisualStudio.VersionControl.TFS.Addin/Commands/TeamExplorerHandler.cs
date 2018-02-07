using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace VisualStudio.VersionControl.TFS.Addin.Commands
{
    public class TeamExplorerHandler : CommandHandler
    {
        protected override void Run ()
        {
        
            Pad pad = null;

            var pads = IdeApp.Workbench.Pads; 
 
            foreach (var p in IdeApp.Workbench.Pads)
            {
                if (string.Equals(p.Id, "VisualStudio.TFS.TeamExplorerPad", System.StringComparison.OrdinalIgnoreCase))
                {
                    pad = p;
                }
            }

            if (pad == null)
            {
                var content = new Gui.Pads.TeamExplorerPad();

                pad = IdeApp.Workbench.ShowPad(content, "VisualStudio.TFS.TeamExplorerPad", "Team Explorer", "Right", null);
               
                if (pad == null)
                    return;
            }

            pad.Sticky = true;
            pad.AutoHide = false;
            pad.BringToFront();
        }

        protected override void Update (CommandInfo info)
        {
            info.Enabled = true;

            base.Update(info);
        }   
    }
}