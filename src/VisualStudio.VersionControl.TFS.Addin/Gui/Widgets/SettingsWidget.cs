using Xwt;
using MonoDevelop.Core;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using System;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    public class SettingsWidget : VBox
    {
        ComboBox _lockLevelBox;
        Button _mergeToolButton;
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
        
            PackStart(new Label(GettextCatalog.GetString("Merge Tool:")));
            _mergeToolButton = new Button(GettextCatalog.GetString("Configure Merge Tool"));
            _mergeToolButton.Clicked += OnConfigMergeTool;
            PackStart(_mergeToolButton);
        }

        public void ApplyChanges()
        {
            TeamFoundationServerClient.Settings.CheckOutLockLevel = (CheckOutLockLevel)_lockLevelBox.SelectedItem;
        }

        ComboBox CreateLockLevelComboBox()
        {
            ComboBox lockLevelBox = new ComboBox();

            lockLevelBox.Items.Add(CheckOutLockLevel.Unchanged, "Keep any existing lock.");
            lockLevelBox.Items.Add(CheckOutLockLevel.CheckOut, "Prevent other users from checking out and checking in");
            lockLevelBox.Items.Add(CheckOutLockLevel.CheckIn, "Prevent other users from checking in but allow checking out");

            if (TeamFoundationServerClient.Settings.CheckOutLockLevel == CheckOutLockLevel.Unchanged)
                lockLevelBox.SelectedItem = CheckOutLockLevel.CheckOut;
            else
                lockLevelBox.SelectedItem = TeamFoundationServerClient.Settings.CheckOutLockLevel;
            

            return lockLevelBox;
        }

        void OnConfigMergeTool(object sender, EventArgs e)
        {
            using (var mergeToolDialog = new MergeToolDialog(TeamFoundationServerClient.Settings.MergeTool))
            {
                if (mergeToolDialog.Run(ParentWindow) == Command.Ok)
                {
                    TeamFoundationServerClient.Settings.MergeTool = mergeToolDialog.MergeToolInfo;
                    TeamFoundationServerClient.Settings.SaveSettings();
                }
            }
        }
    }
}