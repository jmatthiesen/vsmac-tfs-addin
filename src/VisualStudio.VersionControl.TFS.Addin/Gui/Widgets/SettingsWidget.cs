using Autofac;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    public class SettingsWidget : VBox
    {
        ComboBox _lockLevelBox;
        CheckBox _debugModeBox;

        TeamFoundationServerVersionControlService _service;

        public SettingsWidget()
        {
            Init();
            BuildGui();
        }

        void Init()
        {
            _service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            _lockLevelBox = CreateLockLevelComboBox();     
            _debugModeBox = new CheckBox(GettextCatalog.GetString("Debug Mode"));
        }

        void BuildGui()
        {
            PackStart(new Label(GettextCatalog.GetString("Lock Level:")));
            PackStart(_lockLevelBox);

            _debugModeBox.AllowMixed = false;
            _debugModeBox.Active = _service.DebugMode;
            PackStart(_debugModeBox);
        }

        public void ApplyChanges()
        {
            _service.CheckOutLockLevel = (LockLevel)_lockLevelBox.SelectedItem;
            _service.DebugMode = _debugModeBox.Active;
        }

        ComboBox CreateLockLevelComboBox()
        {
            ComboBox lockLevelBox = new ComboBox();

            lockLevelBox.Items.Add(LockLevel.Unchanged, "Keep any existing lock.");
            lockLevelBox.Items.Add(LockLevel.CheckOut, "Prevent other users from checking out and checking in");
            lockLevelBox.Items.Add(LockLevel.Checkin, "Prevent other users from checking in but allow checking out");

            if (_service.CheckOutLockLevel == LockLevel.Unchanged)
                lockLevelBox.SelectedItem = LockLevel.CheckOut;
            else
                lockLevelBox.SelectedItem = _service.CheckOutLockLevel;
            
            return lockLevelBox;
        }
    }
}