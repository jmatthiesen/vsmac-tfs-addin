﻿// CheckOutDialog.cs
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

using System.Collections.Generic;
using Autofac;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
	/// <summary>
    /// Check out dialog.
    /// </summary>
    public class CheckOutDialog : Dialog
    {
        List<ExtendedItem> _items;
        IWorkspaceService _workspace;
        
        ListView _filesView;
        DataField<bool> _isCheckedField;
        DataField<string> _nameField;
        DataField<string> _folderField;
        DataField<ExtendedItem> _itemField;
        ListStore _fileStore;
        ComboBox _lockLevelBox;

        internal CheckOutDialog(List<ExtendedItem> items, IWorkspaceService workspace)
        {
            Init(items, workspace);
            BuildGui();
			LoadData();
        }

        /// <summary>
        /// Gets the selected items.
        /// </summary>
        /// <value>The selected items.</value>
        internal List<ExtendedItem> SelectedItems
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

        /// <summary>
        /// Gets the lock level.
        /// </summary>
        /// <value>The lock level.</value>
        internal LockLevel LockLevel
        {
            get
            {
                return (LockLevel)_lockLevelBox.SelectedItem;
            }
        }

        /// <summary>
		/// Init CheckOutDialog.
        /// </summary>
        /// <param name="items">Items.</param>
        /// <param name="workspace">Workspace.</param>
        void Init(List<ExtendedItem> items, IWorkspaceService workspace)
        {
            _items = items;
            _workspace = workspace;

            _filesView = new ListView();    
            _isCheckedField = new DataField<bool>();
            _nameField = new DataField<string>();
            _folderField = new DataField<string>();  
            _itemField = new DataField<ExtendedItem>();
            _fileStore = new ListStore(_isCheckedField, _nameField, _folderField, _itemField);
            _lockLevelBox = BuildLockLevelComboBox();
        }

        /// <summary>
		/// Builds the CheckOutDialog GUI.
        /// </summary>
        void BuildGui()
        {
            Title = GettextCatalog.GetString("Checkout");

            var content = new VBox();

            content.PackStart(new Label(GettextCatalog.GetString("Files") + ":"));
            _filesView.WidthRequest = 600;
            _filesView.HeightRequest = 200;

            var checkView = new CheckBoxCellView(_isCheckedField)
            {
                Editable = true
            };
            _filesView.Columns.Add("Name", checkView, new TextCellView(_nameField));
            _filesView.Columns.Add("Folder", _folderField);
            _filesView.DataSource = _fileStore;
            content.PackStart(_filesView, true, true);

            var lockBox = new HBox();
            lockBox.PackStart(new Label(GettextCatalog.GetString("Select lock level") + ":"));
            lockBox.PackStart(_lockLevelBox, true, true);
            content.PackStart(lockBox);

            Buttons.Add(Command.Ok, Command.Cancel);
            Content = content;
            Resizable = false;
        }

        /// <summary>
        /// Builds the lock level combo box.
        /// </summary>
        /// <returns>The lock level combo box.</returns>
        ComboBox BuildLockLevelComboBox()
        {
            ComboBox lockLevelBox = new ComboBox { WidthRequest = 150 };

            lockLevelBox.Items.Add(LockLevel.Unchanged, "Unchanged - Keep any existing lock.");
            lockLevelBox.Items.Add(LockLevel.CheckOut, "Check Out - Prevent other users from checking out and checking in");
            lockLevelBox.Items.Add(LockLevel.Checkin, "Check In - Prevent other users from checking in but allow checking out");

            var service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

			if (service.CheckOutLockLevel == LockLevel.Unchanged)
			{
				lockLevelBox.SelectedItem = LockLevel.CheckOut;
			}
			else
			{
				lockLevelBox.SelectedItem = service.CheckOutLockLevel;
			}

            return lockLevelBox;
        }

        /// <summary>
		/// Loads the data (checkout files).
        /// </summary>
        void LoadData()
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