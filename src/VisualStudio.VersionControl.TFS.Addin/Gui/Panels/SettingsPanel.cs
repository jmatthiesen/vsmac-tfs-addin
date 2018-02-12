using MonoDevelop.Components;
using VisualStudio.VersionControl.TFS.Addin.Gui.Widgets;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Panels
{
    public class SettingsPanel : MonoDevelop.Ide.Gui.Dialogs.OptionsPanel
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