// CheckInDialog.cs
// 
// Author:
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2018 Javier Suárez Ruiz
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

using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
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
        Button _addWorkItemButton;
        Label _workItemLabel;
        WorkItem _workItem;

        internal CheckInDialog(IEnumerable<BaseItem> items, IWorkspace workspace)
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

        internal Dictionary<int, WorkItemCheckinAction> SelectedWorkItems
        {
            get
            {
                if (_workItem != null)
                {
                    var items = new Dictionary<int, WorkItemCheckinAction>
                    {
                        { _workItem.Id, WorkItemCheckinAction.Associate }
                    };

                    return items;
                }
                else
                {
                    return null;    
                }
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
                    
            _content.PackStart(new Label(GettextCatalog.GetString("Work Item") + ":"));
            var workItemButtonBox = new VBox();
            _addWorkItemButton = new Button(GettextCatalog.GetString("Add Work Item"));
            _addWorkItemButton.Clicked += OnAddWorkItem;
            workItemButtonBox.PackStart(_addWorkItemButton);
            _workItemLabel = new Label();
            _workItemLabel.Visible = false;
            workItemButtonBox.PackStart(_workItemLabel);
            _content.PackStart(workItemButtonBox);

            Buttons.Add(Command.Ok, Command.Cancel);

            Content = _content;
            Resizable = false; 
        }

        void GetData(IEnumerable<BaseItem> items, IWorkspace workspace)
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
                var path = pendingChange.ServerItem;
                _fileStore.SetValue(row, _nameField, path.ItemName);
                _fileStore.SetValue(row, _changesField, pendingChange.ChangeType.ToString());
                _fileStore.SetValue(row, _folderField, path.ParentPath);
                _fileStore.SetValue(row, _changeField, pendingChange);
            }
        }

        void OnAddWorkItem(object sender, EventArgs e)
        {
            using (var selectWorkItemDialog = new ChooseWorkItemDialog())
            {
                if (selectWorkItemDialog.Run() == Command.Ok)
                {
                    var workItem = selectWorkItemDialog.WorkItem;

                    if(workItem != null)
                    {
                        _addWorkItemButton.Visible = false;
                        _workItemLabel.Text = string.Format("{0} - {1}", workItem.Id, workItem.WorkItemInfo["System.Title"]);
                        _workItemLabel.Visible = true;

                        _workItem = workItem;
                    }
                    else
                    {
                        _addWorkItemButton.Visible = true;
                        _workItemLabel.Visible = false;
                    }
                }
            }
        }
    }
}