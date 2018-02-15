using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Core;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs
{
    public class CheckInDialog : Dialog
    {
        VBox _content;
        ListView _filesView;
        DataField<bool> _isCheckedField;
        DataField<string> _nameField;
        DataField<string> _changesField;
        DataField<string> _folderField;
        DataField<PendingChange> _changeField;
        ListStore _fileStore;
        TextEntry _commentEntry;

        public CheckInDialog(List<ExtendedItem> items, Workspace workspace)
        {
            Init();
            BuildGui();
            GetData(items, workspace);
        }

        internal List<PendingChange> SelectedChanges
        {
            get
            {
                var items = new List<PendingChange>();
               
                for (int i = 0; i < _fileStore.RowCount; i++)
                {
                    var isChecked = _fileStore.GetValue(i, _isCheckedField);

                    if (isChecked)
                        items.Add(_fileStore.GetValue(i, _changeField));
                }

                return items;
            }
        }

        internal string Comment
        {
            get
            {
                return _commentEntry.Text;
            }
        }

        void Init()
        {
            _content = new VBox();
            _filesView = new ListView();
            _isCheckedField = new DataField<bool>();
            _nameField = new DataField<string>();
            _changesField = new DataField<string>();
            _folderField = new DataField<string>();
            _changeField = new DataField<PendingChange>();
            _fileStore = new ListStore(_isCheckedField, _nameField, _changesField, _folderField, _changeField);
            _commentEntry = new TextEntry();
            _commentEntry.MultiLine = true;
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Check In files");
                
            _filesView.WidthRequest = 600;
            _filesView.HeightRequest = 150;
            var checkView = new CheckBoxCellView(_isCheckedField);
            checkView.Editable = true;
            _filesView.Columns.Add("Name", checkView, new TextCellView(_nameField));
            _filesView.Columns.Add("Changes", _changesField);
            _filesView.Columns.Add("Folder", _folderField);
            _filesView.DataSource = _fileStore;
            _content.PackStart(new Label(GettextCatalog.GetString("Pending Changes") + ":"));
            _content.PackStart(_filesView, true, true);

            _content.PackStart(new Label(GettextCatalog.GetString("Comment") + ":"));
            _commentEntry.MultiLine = true;
            _content.PackStart(_commentEntry);
                    
            Buttons.Add(Command.Ok, Command.Cancel);

            Content = _content;
            Resizable = false; 
        }

        void GetData(List<ExtendedItem> items, Workspace workspace)
        {
            _fileStore.Clear();
            List<ItemSpec> itemSpecs = new List<ItemSpec>();
          
            foreach (var item in items)
            {
                itemSpecs.Add(new ItemSpec(item.ServerPath, item.ItemType == ItemType.File ? RecursionType.None : RecursionType.Full));
            }

            var pendingChanges = workspace.GetPendingChanges(itemSpecs);

            foreach (var pendingChange in pendingChanges)
            {
                var row = _fileStore.AddRow();
                _fileStore.SetValue(row, _isCheckedField, true);
                var path = (VersionControlPath)pendingChange.ServerItem;
                _fileStore.SetValue(row, _nameField, path.ItemName);
                _fileStore.SetValue(row, _changesField, pendingChange.ChangeType.ToString());
                _fileStore.SetValue(row, _folderField, path.ParentPath);
                _fileStore.SetValue(row, _changeField, pendingChange);
            }
        }
    }
}