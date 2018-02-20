using MonoDevelop.Components;
using MonoDevelop.VersionControl.TFS.Gui.Widgets;

namespace MonoDevelop.VersionControl.TFS.Gui.Panels
{
    public class SettingsPanel : Ide.Gui.Dialogs.OptionsPanel
    {
        SettingsWidget _widget;

        public override void ApplyChanges()
        {
            _widget.ApplyChanges();
        }

        public override Control CreatePanelWidget()
        {
            _widget = new SettingsWidget();

            return new XwtControl(_widget);
        }
    }
}