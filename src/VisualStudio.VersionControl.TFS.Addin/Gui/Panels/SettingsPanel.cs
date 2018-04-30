using MonoDevelop.Components;
using MonoDevelop.VersionControl.TFS.Gui.Widgets;

namespace MonoDevelop.VersionControl.TFS.Gui.Panels
{
	/// <summary>
    /// Settings panel.
    /// </summary>
    public class SettingsPanel : Ide.Gui.Dialogs.OptionsPanel
    {
        SettingsWidget _widget;

        public override void ApplyChanges()
        {
            _widget.ApplyChanges();
        }
        
        /// <summary>
        /// Creates the panel widget.
        /// </summary>
        /// <returns>The panel widget.</returns>
        public override Control CreatePanelWidget()
        {
            _widget = new SettingsWidget();

            return new XwtControl(_widget);
        }
    }
}