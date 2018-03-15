using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class UndoDialog : Dialog
    {
        ListView _filesView;
        DataField<bool> _isCheckedField = new DataField<bool>();
        DataField<string> _nameField = new DataField<string>();
        DataField<string> _changesField = new DataField<string>();
        DataField<string> _folderField = new DataField<string>();
        DataField<PendingChange> _changeField = new DataField<PendingChange>();
        ListStore _filesStore;

        internal UndoDialog(List<ExtendedItem> items, IWorkspace workspace)
        {
            Init();
            BuildGui();
            GetData(items, workspace);
        }

        internal List<PendingChange> SelectedItems
        {
            get
            {
                var items = new List<PendingChange>();

                for (int i = 0; i < _filesStore.RowCount; i++)
                {
                    var isChecked = _filesStore.GetValue(i, _isCheckedField);

                    if (isChecked)
                        items.Add(_filesStore.GetValue(i, _changeField));
                }

                return items;
            }
        }

        void Init()
        {
            _filesView = new ListView();    
            _isCheckedField = new DataField<bool>();
            _nameField = new DataField<string>();
            _changesField = new DataField<string>();
            _folderField = new DataField<string>();
            _changeField = new DataField<PendingChange>();
            _filesStore = new ListStore(_isCheckedField, _nameField, _changesField, _folderField, _changeField);
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Undo changes");
           
            var content = new VBox();
            content.PackStart(new Label(GettextCatalog.GetString("Files") + ":"));
            _filesView.WidthRequest = 600;
            _filesView.HeightRequest = 150;

            var checkView = new CheckBoxCellView(_isCheckedField);
            checkView.Editable = true;

            _filesView.Columns.Add("Name", checkView, new TextCellView(_nameField));
            _filesView.Columns.Add("Changes", _changesField);
            _filesView.Columns.Add("Folder", _folderField);
            _filesView.DataSource = _filesStore;

            content.PackStart(_filesView, true, true);    
            Content = content;

            Buttons.Add(Command.Ok, Command.Cancel);
           
            Resizable = false;
        }

        void GetData(List<ExtendedItem> items, IWorkspace workspace)
        {
            _filesStore.Clear();
            /*
            List<ItemSpec> itemSpecs = new List<ItemSpec>();
           
            foreach (var item in items)
            {
                itemSpecs.Add(new ItemSpec(item.ServerPath, item.ItemType == ItemType.File ? RecursionType.None : RecursionType.Full));
            }

            var pendingChanges = workspace.GetPendingChanges(itemSpecs);

            foreach (var pendingChange in pendingChanges)
            {
                var row = _filesStore.AddRow();
                _filesStore.SetValue(row, _isCheckedField, true);
                var path = (VersionControlPath)pendingChange.ServerItem;

                _filesStore.SetValue(row, _nameField, path.ItemName);
                _filesStore.SetValue(row, _changesField, pendingChange.ChangeType.ToString());
                _filesStore.SetValue(row, _folderField, path.ParentPath);
                _filesStore.SetValue(row, _changeField, pendingChange);
            }
            */
        }
    }
}