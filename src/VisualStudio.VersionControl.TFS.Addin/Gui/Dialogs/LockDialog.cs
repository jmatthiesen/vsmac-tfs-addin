// LockDialog.cs
// 
// Author:
//       Ventsislav Mladenov
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2018 Ventsislav Mladenov, Javier Suárez Ruiz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class LockDialog: Dialog
    {
        ListView _fileView;
        DataField<bool> _isCheckedField;
        DataField<string> _nameField;
        DataField<string> _folderField;
        DataField<ExtendedItem> _itemField;
        ListStore _fileStore;
  
        public LockDialog(List<ExtendedItem> items)
        {
            Init();
            BuildGui();
            GetData(items);
        }

        internal List<ExtendedItem> SelectedItems
        {
            get
            {
                var items = new List<ExtendedItem>();

                for (int i = 0; i < _fileStore.RowCount; i++)
                {
                    var isChecked = _fileStore.GetValue(i, _isCheckedField);

                    if (isChecked)
                    {
                        items.Add(_fileStore.GetValue(i, _itemField));
                    }
                }

                return items;
            }
        }

        void Init()
        {
            _fileView = new ListView();
            _isCheckedField = new DataField<bool>();
            _nameField = new DataField<string>();
            _folderField = new DataField<string>();
            _itemField = new DataField<ExtendedItem>();
            _fileStore = new ListStore(_isCheckedField, _nameField, _folderField, _itemField);
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Lock Files");

            var content = new VBox();
            content.PackStart(new Label(GettextCatalog.GetString("Files") + ":"));
            _fileView.WidthRequest = 500;
            _fileView.HeightRequest = 150;
            var checkView = new CheckBoxCellView(_isCheckedField);
            checkView.Editable = true;
            _fileView.Columns.Add("Name", checkView, new TextCellView(_nameField));
            _fileView.Columns.Add("Folder", _folderField);
            _fileView.DataSource = _fileStore;
            content.PackStart(_fileView, true, true);

            Buttons.Add(Command.Ok, Command.Cancel);

            Content = content;
            Resizable = false;
        }

        void GetData(List<ExtendedItem> items)
        {
            _fileStore.Clear();
            foreach (var item in items)
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