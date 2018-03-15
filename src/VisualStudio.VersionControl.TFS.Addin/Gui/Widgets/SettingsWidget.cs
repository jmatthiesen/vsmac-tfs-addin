using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    public class SettingsWidget : VBox
    {
        ComboBox _lockLevelBox;
        
        public SettingsWidget()
        {
            Init();
            BuildGui();
        }

        void Init()
        {
            _lockLevelBox = CreateLockLevelComboBox();
        }

        void BuildGui()
        {
            PackStart(new Label(GettextCatalog.GetString("Lock Level:")));
            PackStart(_lockLevelBox);
        }

        public void ApplyChanges()
        {
            //TeamFoundationServerClient.Settings.CheckOutLockLevel = (CheckOutLockLevel)_lockLevelBox.SelectedItem;
        }

        ComboBox CreateLockLevelComboBox()
        {
            ComboBox lockLevelBox = new ComboBox();

            /*
            lockLevelBox.Items.Add(CheckOutLockLevel.Unchanged, "Keep any existing lock.");
            lockLevelBox.Items.Add(CheckOutLockLevel.CheckOut, "Prevent other users from checking out and checking in");
            lockLevelBox.Items.Add(CheckOutLockLevel.CheckIn, "Prevent other users from checking in but allow checking out");

            if (TeamFoundationServerClient.Settings.CheckOutLockLevel == CheckOutLockLevel.Unchanged)
                lockLevelBox.SelectedItem = CheckOutLockLevel.CheckOut;
            else
                lockLevelBox.SelectedItem = TeamFoundationServerClient.Settings.CheckOutLockLevel;
            */

            return lockLevelBox;
        }
    }
}