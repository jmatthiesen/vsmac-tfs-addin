using Xwt;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    public class SettingsWidget : VBox
    {
        public SettingsWidget()
        {
            BuildGui();
        }

        void BuildGui()
        {
            PackStart(new Label(GettextCatalog.GetString("v0.1")));
        }

        public void ApplyChanges()
        {
            
        }
    }
}