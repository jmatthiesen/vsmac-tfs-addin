// UndoDialog.cs
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
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
	/// <summary>
    /// Undo dialog.
    /// </summary>
    public class UndoDialog : Dialog
    {
        ListView _filesView;
        DataField<bool> _isCheckedField = new DataField<bool>();
        DataField<string> _nameField = new DataField<string>();
        DataField<string> _changesField = new DataField<string>();
        DataField<string> _folderField = new DataField<string>();
        DataField<PendingChange> _changeField = new DataField<PendingChange>();
        ListStore _filesStore;

        internal UndoDialog(List<ExtendedItem> items, IWorkspaceService workspace)
        {
            Init();
            BuildGui();
			LoadData(items, workspace);
        }

        /// <summary>
        /// Gets the selected items.
        /// </summary>
        /// <value>The selected items.</value>
        internal List<PendingChange> SelectedItems
        {
            get
            {
                var items = new List<PendingChange>();

				for (int i = 0; i < _filesStore.RowCount; i++)
				{
					var isChecked = _filesStore.GetValue(i, _isCheckedField);

					if (isChecked)
					{
						items.Add(_filesStore.GetValue(i, _changeField));
					}
				}

                return items;
            }
        }

        /// <summary>
		/// Init UndoDialog.
        /// </summary>
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

        /// <summary>
		/// Builds the UndoDialog GUI.
        /// </summary>
        void BuildGui()
        {
            Title = GettextCatalog.GetString("Undo changes");
           
            var content = new VBox();
            content.PackStart(new Label(GettextCatalog.GetString("Files") + ":"));
            _filesView.WidthRequest = 600;
            _filesView.HeightRequest = 150;

			var checkView = new CheckBoxCellView(_isCheckedField)
			{
				Editable = true
			};

			_filesView.Columns.Add("Name", checkView, new TextCellView(_nameField));
            _filesView.Columns.Add("Changes", _changesField);
            _filesView.Columns.Add("Folder", _folderField);
            _filesView.DataSource = _filesStore;

            content.PackStart(_filesView, true, true);    
            Content = content;

            Buttons.Add(Command.Ok, Command.Cancel);
           
            Resizable = false;
        }

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="items">Items.</param>
        /// <param name="workspace">Workspace.</param>
        void LoadData(List<ExtendedItem> items, IWorkspaceService workspace)
        {
            _filesStore.Clear();

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
                var path = pendingChange.ServerItem;

                _filesStore.SetValue(row, _nameField, path.ItemName);
                _filesStore.SetValue(row, _changesField, pendingChange.ChangeType.ToString());
                _filesStore.SetValue(row, _folderField, path.ParentPath);
                _filesStore.SetValue(row, _changeField, pendingChange);
            }
        }
    }
}