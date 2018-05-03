// TFSCommitDialogExtensionWidget.cs
// 
// Authors:
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

using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
	/// <summary>
    /// Team foundation server CommitDialogExtension Widget.
    /// </summary>
	public class TeamFoundationServerCommitDialogExtensionWidget : HBox
	{
		TreeView _workItemsView;
		ListStore _workItemStore;
		ListStore _checkinActions;
		Button _removeButton;

		public TeamFoundationServerCommitDialogExtensionWidget()
		{
			Init();
			BuildGui();
			AttachEvents();
		}
        
        /// <summary>
        /// Gets the work items.
        /// </summary>
        /// <value>The work items.</value>
        public Dictionary<int, WorkItemCheckinAction> WorkItems
        {
            get
            {
                var workItems = new Dictionary<int, WorkItemCheckinAction>();

                if (_workItemStore.GetIterFirst(out TreeIter iter))
                {
                    var value = GetValue(iter);
                    workItems.Add(value.Key, value.Value);

                    while (_workItemStore.IterNext(ref iter))
                    {
                        var valueNext = GetValue(iter);
                        workItems.Add(valueNext.Key, valueNext.Value);
                    }
                }

                return workItems;
            }
        }

        /// <summary>
		/// Init TeamFoundationServerCommitDialogExtensionWidget.
        /// </summary>
        void Init()
		{
			_workItemsView = new TreeView();
			_workItemStore = new ListStore(typeof(int), typeof(string), typeof(string));
			_checkinActions = new ListStore(typeof(string));
			_removeButton = new Button();
		}

        /// <summary>
		/// Builds the TeamFoundationServerCommitDialogExtensionWidget GUI.
        /// </summary>
		void BuildGui()
		{
			CellRendererText cellId = new CellRendererText();

			TreeViewColumn idColumn = new TreeViewColumn
			{
				Title = GettextCatalog.GetString("ID")
			};

			idColumn.PackStart(cellId, false);
			idColumn.AddAttribute(cellId, "text", 0);

			CellRendererText cellTitle = new CellRendererText();
		
			TreeViewColumn titleColumn = new TreeViewColumn
			{
				Title = "Title",
				Expand = true,
				Sizing = TreeViewColumnSizing.Fixed
			};

			titleColumn.PackStart(cellTitle, true);
			titleColumn.AddAttribute(cellTitle, "text", 1);

			CellRendererCombo cellAction = new CellRendererCombo();

			TreeViewColumn actionColumn = new TreeViewColumn
			{
				Title = "Action"
			};

			actionColumn.PackStart(cellAction, false);
			actionColumn.AddAttribute(cellAction, "text", 2);
			cellAction.Editable = true;
			cellAction.Model = _checkinActions;
			cellAction.TextColumn = 0;
			cellAction.HasEntry = false;
			cellAction.Edited += OnActionChanged;

			_checkinActions.AppendValues(WorkItemCheckinAction.Associate.ToString());
           
			_workItemsView.AppendColumn(idColumn);
			_workItemsView.AppendColumn(titleColumn);
			_workItemsView.AppendColumn(actionColumn);

			_workItemsView.Model = _workItemStore;
			_workItemsView.WidthRequest = 300;
			_workItemsView.HeightRequest = 120;

			PackStart(_workItemsView, true, true, 3);

			VButtonBox buttonBox = new VButtonBox();
		
			Button addButton = new Button
			{
				Label = GettextCatalog.GetString("Add Work Item")
			};

			addButton.Clicked += OnAddWorkItem;
			_removeButton.Label = GettextCatalog.GetString("Remove Work Item");
			_removeButton.Sensitive = false;

			addButton.WidthRequest = _removeButton.WidthRequest = 150;

			buttonBox.PackStart(addButton);
			buttonBox.PackStart(_removeButton);
			buttonBox.Layout = ButtonBoxStyle.Start;

			PackStart(buttonBox, false, false, 3);

			ShowAll();
		}

		/// <summary>
		/// Attachs the events.
		/// </summary>
		void AttachEvents()
		{
			_removeButton.Clicked += OnRemoveWorkItem;
		}

        /// <summary>
        /// Remove WorkItem.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
		void OnRemoveWorkItem(object sender, EventArgs e)
		{
			TreeSelection selection = _workItemsView.Selection;

			if (!selection.GetSelected(out TreeIter iter))
			{
				return;
			}

			_workItemStore.Remove(ref iter);
			UpdateRemoveWorkItem();
		}

        /// <summary>
        /// Associate commit with a WorkItem.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
		void OnAddWorkItem(object sender, EventArgs e)
        {
            using (var selectWorkItemDialog = new ChooseWorkItemDialog())
            {
                if (selectWorkItemDialog.Run() == Xwt.Command.Ok)
                {
                    var workItem = selectWorkItemDialog.WorkItem;

                    if (workItem != null)
                    {
                        string title = string.Empty;

                        if (workItem.WorkItemInfo.ContainsKey("System.Title"))
                        {
                            title = Convert.ToString(workItem.WorkItemInfo["System.Title"]);
                        }

                        _workItemStore.AppendValues(workItem.Id, title, "Associate");
                    }

					UpdateRemoveWorkItem();
                }
            }
        }

        /// <summary>
        /// Determines if the WorkItem is added.
        /// </summary>
        /// <returns><c>true</c>, if work item added was ised, <c>false</c> otherwise.</returns>
        /// <param name="workItemId">Work item identifier.</param>
		bool IsWorkItemAdded(int workItemId)
		{
			if (_workItemStore.GetIterFirst(out TreeIter iter))
			{
				var id = (int)_workItemStore.GetValue(iter, 0);

				if (id == workItemId)
					return true;
				
				while (_workItemStore.IterNext(ref iter))
				{
					var idNext = (int)_workItemStore.GetValue(iter, 0);

					if (idNext == workItemId)
						return true;
				}
			}

			return false;
		}

		private void RemoveWorkItem(int workItemId)
		{
			if (_workItemStore.GetIterFirst(out TreeIter iter))
			{
				var id = (int)_workItemStore.GetValue(iter, 0);

				if (id == workItemId)
				{
					_workItemStore.Remove(ref iter);
					return;
				}

				while (_workItemStore.IterNext(ref iter))
				{
					var idNext = (int)_workItemStore.GetValue(iter, 0);

					if (idNext == workItemId)
					{
						_workItemStore.Remove(ref iter);
						return;
					}
				}
			}
		}

		void OnActionChanged(object o, EditedArgs args)
		{
			TreeSelection selection = _workItemsView.Selection;

			if (!selection.GetSelected(out TreeIter iter))
			{
				return;
			}
            
			_workItemStore.SetValue(iter, 2, args.NewText);
		}

		KeyValuePair<int, WorkItemCheckinAction> GetValue(TreeIter iter)
		{
			var id = (int)_workItemStore.GetValue(iter, 0);
			var checkinAction = (WorkItemCheckinAction)Enum.Parse(typeof(WorkItemCheckinAction), (string)_workItemStore.GetValue(iter, 2));
			return new KeyValuePair<int, WorkItemCheckinAction>(id, checkinAction);
		}

		void UpdateRemoveWorkItem()
        {
            _removeButton.Sensitive = _workItemStore.IterNChildren() > 0;
        }
	}
}