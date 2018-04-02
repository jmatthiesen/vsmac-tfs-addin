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
        Notebook _notebook;
        ListView _filesView;
        DataField<bool> _isCheckedField;
        DataField<string> _nameField;
        DataField<string> _changesField;
        DataField<string> _folderField;
        DataField<PendingChange> _changeField;
        ListStore _fileStore;
        TextEntry _commentEntry;
        ListView _workItemsView;
        DataField<WorkItem> _workItemField;
        DataField<int> _idField;
        DataField<string> _titleField;
        ListStore _workItemsStore;
        Button _removeWorkItemButton;

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
                var items = new Dictionary<int, WorkItemCheckinAction>();
            
                for (int i = 0; i < _workItemsStore.RowCount; i++)
                {
                    var workItem = _workItemsStore.GetValue(i, _workItemField);
                    items.Add(workItem.Id, WorkItemCheckinAction.Associate);
                }

                return items;
            }
        }

        void Init()
        {
            _notebook = new Notebook();
            _filesView = new ListView();
            _isCheckedField = new DataField<bool>();
            _nameField = new DataField<string>();
            _changesField = new DataField<string>();
            _folderField = new DataField<string>();
            _changeField = new DataField<PendingChange>();
            _fileStore = new ListStore(_isCheckedField, _nameField, _changesField, _folderField, _changeField);
            _commentEntry = new TextEntry();
            _commentEntry.MultiLine = true;
            _workItemsView = new ListView();
            _workItemField = new DataField<WorkItem>();
            _idField = new DataField<int>();
            _titleField = new DataField<string>();
            _workItemsStore = new ListStore(_idField, _titleField, _workItemField);
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Check In files");
                
            _notebook.TabOrientation = NotebookTabOrientation.Left;

            var checkInTab = new VBox();
            _filesView.WidthRequest = 600;
            _filesView.HeightRequest = 150;
            var checkView = new CheckBoxCellView(_isCheckedField);
            checkView.Editable = true;
            _filesView.Columns.Add("Name", checkView, new TextCellView(_nameField));
            _filesView.Columns.Add("Changes", _changesField);
            _filesView.Columns.Add("Folder", _folderField);
            _filesView.DataSource = _fileStore;
            checkInTab.PackStart(new Label(GettextCatalog.GetString("Pending Changes") + ":"));
            checkInTab.PackStart(_filesView, true, true);

            checkInTab.PackStart(new Label(GettextCatalog.GetString("Comment") + ":"));
            _commentEntry.MultiLine = true;
            checkInTab.PackStart(_commentEntry);
                    
            _notebook.Add(checkInTab, GettextCatalog.GetString("Pending Changes"));

            var workItemsTab = new HBox();
            var workItemsListBox = new VBox();
            workItemsListBox.PackStart(new Label(GettextCatalog.GetString("Work Items") + ":"));
            _workItemsView.Columns.Add("Id", _idField);
            _workItemsView.Columns.Add("Title", _titleField);
            _workItemsView.DataSource = _workItemsStore;
            _workItemsView.SelectionChanged += (sender, args) => UpdateRemoveWorkItem();
            workItemsListBox.PackStart(_workItemsView, true);
            workItemsTab.PackStart(workItemsListBox, true, true);

            var workItemButtonBox = new VBox();
            var addWorkItemButton = new Button(GettextCatalog.GetString("Add Work Item"));
            addWorkItemButton.Clicked += OnAddWorkItem;
            workItemButtonBox.PackStart(addWorkItemButton);

            _removeWorkItemButton = new Button(GettextCatalog.GetString("Remove Work Item"));
            _removeWorkItemButton.Clicked += OnRemoveWorkItem;
            workItemButtonBox.PackStart(_removeWorkItemButton);

            addWorkItemButton.MinWidth = _removeWorkItemButton.MinWidth = GuiSettings.ButtonWidth;

            workItemsTab.PackStart(workItemButtonBox);

            _notebook.Add(workItemsTab, GettextCatalog.GetString("Work Items"));

            Buttons.Add(Command.Ok, Command.Cancel);

            Content = _notebook;
            Resizable = false; 

            UpdateRemoveWorkItem();
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
            using (var chooseWorkItemDialog = new ChooseWorkItemDialog())
            {
                chooseWorkItemDialog.OnSelectWorkItem += (workItem) =>
                {
                    if (IsWorkItemAdded(workItem.Id))
                    { 
                        return;
                    }

                    string title = string.Empty;

                    if (workItem.WorkItemInfo.ContainsKey("System.Title"))
                    {
                        title = Convert.ToString(workItem.WorkItemInfo["System.Title"]);
                    }

                    var row = _workItemsStore.AddRow();
                    _workItemsStore.SetValue(row, _workItemField, workItem);
                    _workItemsStore.SetValue(row, _idField, workItem.Id);
                    _workItemsStore.SetValue(row, _titleField, title);
                };

                chooseWorkItemDialog.Run();
            }
        }

        bool IsWorkItemAdded(int workItemId)
        {
            for (int i = 0; i < _workItemsStore.RowCount; i++)
            {
                var workItem = _workItemsStore.GetValue(i, _workItemField);

                if (workItem.Id == workItemId)
                    return true;
            }

            return false;
        }

        void OnRemoveWorkItem(object sender, EventArgs e)
        {
            if (_workItemsView.SelectedRow > -1)
            {
                _workItemsStore.RemoveRow(_workItemsView.SelectedRow);
            }
        }

        void UpdateRemoveWorkItem()
        {
            _removeWorkItemButton.Sensitive = _workItemsView.SelectedRow != -1;  
        }
    }
}