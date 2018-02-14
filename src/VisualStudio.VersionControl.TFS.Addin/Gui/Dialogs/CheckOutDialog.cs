using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Core;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs
{
    public class CheckOutDialog : Dialog
    {
        List<ExtendedItem> _items;
        Workspace _workspace;
        
        ListView _filesView;
        DataField<bool> _isCheckedField;
        DataField<string> _nameField;
        DataField<string> _folderField;
        DataField<ExtendedItem> _itemField;
        ListStore _fileStore;

        public CheckOutDialog(List<ExtendedItem> items, Workspace workspace)
        {
            Init(items, workspace);
            BuildGui();
            GetData();
        }

        public List<ExtendedItem> SelectedItems
        {
            get
            {
                var items = new List<ExtendedItem>();
               
                for (int i = 0; i < _fileStore.RowCount; i++)
                {
                    var isChecked = _fileStore.GetValue(i, _isCheckedField);
                
                    if (isChecked)
                        items.Add(_fileStore.GetValue(i, _itemField));
                }

                return items;
            }
        }

        void Init(List<ExtendedItem> items, Workspace workspace)
        {
            _items = items;
            _workspace = workspace;

            _filesView = new ListView();    
            _isCheckedField = new DataField<bool>();
            _nameField = new DataField<string>();
            _folderField = new DataField<string>();  
            _itemField = new DataField<ExtendedItem>();
            _fileStore = new ListStore(_isCheckedField, _nameField, _folderField, _itemField);
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Check Out");

            var content = new VBox();

            content.PackStart(new Label(GettextCatalog.GetString("Files") + ":"));
            _filesView.WidthRequest = 600;
            _filesView.HeightRequest = 200;

            var checkView = new CheckBoxCellView(_isCheckedField);
            checkView.Editable = true;
            _filesView.Columns.Add("Name", checkView, new TextCellView(_nameField));
            _filesView.Columns.Add("Folder", _folderField);
            _filesView.DataSource = _fileStore;
            content.PackStart(_filesView, true, true);

            Buttons.Add(Command.Ok, Command.Cancel);
            Content = content;
            Resizable = false;
        }

        void GetData()
        {
            _fileStore.Clear();

            foreach (var item in _items)
            {
                var row = _fileStore.AddRow();
                _fileStore.SetValue(row, _isCheckedField, true);
                _fileStore.SetValue(row, _nameField, item.ServerPath.ItemName);
                _fileStore.SetValue(row, _folderField, item.ServerPath.ParentPath);
                _fileStore.SetValue(row, _itemField, item);
            }
        }
    }
}
