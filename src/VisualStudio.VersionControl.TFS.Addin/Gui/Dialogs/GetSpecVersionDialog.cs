using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Core;
using MonoDevelop.Ide.ProgressMonitoring;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs
{
    public class GetSpecVersionDialog : Dialog
    {
        Workspace _workspace;

        ListView _listView;
        DataField<ExtendedItem> _itemField;
        DataField<bool> _isSelectedField;
        DataField<string> _nameField;
        DataField<string> _pathField;
        ListStore _listStore;
        ComboBox _versionBox;
        SpinButton _changeSetNumber;

        public GetSpecVersionDialog(Workspace workspace)
        {
            Init(workspace);
            BuildGui();    
        }

        void Init(Workspace workspace)
        {
            _workspace = workspace;

            _listView = new ListView();
            _itemField = new DataField<ExtendedItem>();
            _isSelectedField = new DataField<bool>();
            _nameField = new DataField<string>();  
            _pathField = new DataField<string>();
            _listStore = new ListStore(_itemField, _isSelectedField, _nameField, _pathField);
            _versionBox = new ComboBox();
            _changeSetNumber = new SpinButton();
        }

        void BuildGui()
        {
            Title = "Get";

            VBox content = new VBox();

            content.PackStart(new Label(GettextCatalog.GetString("Files") + ":"));

            var checkSell = new CheckBoxCellView(_isSelectedField);
            checkSell.Editable = true;
            _listView.Columns.Add("Name", checkSell, new TextCellView(_nameField));
            _listView.Columns.Add("Folder", new TextCellView(_pathField));
            _listView.MinHeight = 300;
            _listView.MinWidth = 300;
            _listView.DataSource = _listStore;

            content.PackStart(_listView);

            HBox typeBox = new HBox();
            typeBox.PackStart(new Label(GettextCatalog.GetString("Version") + ":"));
            _versionBox.Items.Add(0, "Changeset");
            _versionBox.Items.Add(1, "Latest Version");
            _versionBox.SelectedItem = 1;
            _versionBox.SelectionChanged += (sender, e) => _changeSetNumber.Visible = (int)_versionBox.SelectedItem == 0;
            typeBox.PackStart(_versionBox);
            _changeSetNumber.Visible = false;
            _changeSetNumber.WidthRequest = 100;
            _changeSetNumber.MinimumValue = 1;
            _changeSetNumber.MaximumValue = int.MaxValue;
            _changeSetNumber.Value = 0;
            _changeSetNumber.IncrementValue = 1;
            _changeSetNumber.Digits = 0;
            typeBox.PackStart(_changeSetNumber);
            content.PackStart(typeBox);

            HBox buttonBox = new HBox();
            Button okButton = new Button(GettextCatalog.GetString("Get"));
            okButton.Clicked += OnGet;
            Button cancelButton = new Button(GettextCatalog.GetString("Cancel"));
            cancelButton.Clicked += (sender, e) => Respond(Command.Cancel);
            okButton.WidthRequest = cancelButton.WidthRequest = GuiSettings.ButtonWidth;

            buttonBox.PackEnd(cancelButton);
            buttonBox.PackEnd(okButton);
            content.PackStart(buttonBox);

            Content = content;
            Resizable = false;
        }

        public void AddData(List<ExtendedItem> items)
        {
            _listStore.Clear();

            foreach (var item in items)
            {
                var row = _listStore.AddRow();
                _listStore.SetValue(row, _itemField, item);
                _listStore.SetValue(row, _isSelectedField, true);
                VersionControlPath path = item.TargetServerItem;
                _listStore.SetValue(row, _nameField, path.ItemName);
                _listStore.SetValue(row, _pathField, path.ParentPath);
            }
        }

        void OnGet(object sender, EventArgs e)
        {
            var requests = new List<GetRequest>();

            for (int row = 0; row < _listStore.RowCount; row++)
            {
                var isChecked = _listStore.GetValue(row, _isSelectedField);
             
                if (isChecked)
                {
                    var item = _listStore.GetValue(row, _itemField);
                    var spec = new ItemSpec(item.ServerPath, item.ItemType == ItemType.File ? RecursionType.None : RecursionType.Full);
                    var version = (int)_versionBox.SelectedItem == 0 ? new ChangesetVersionSpec(Convert.ToInt32(_changeSetNumber.Value)) : VersionSpec.Latest;

                    requests.Add(new GetRequest(spec, version));

                    // Force Get 
                    _workspace.ResetDownloadStatus(item.ItemId);

                }
            }

            Respond(Command.Ok);

            var option =  GetOptions.GetAll;

            using (var progress = new MessageDialogProgressMonitor(true, false, true))
            {
                progress.Log.WriteLine("Start downloading items. GetOption: " + option);
              
                foreach (var request in requests)
                {
                    progress.Log.WriteLine(request);
                }

                TeamFoundationServerClient.Instance.Get(_workspace, requests, option, progress);
                progress.ReportSuccess("Finish Downloading.");
            }
        }
    }
}