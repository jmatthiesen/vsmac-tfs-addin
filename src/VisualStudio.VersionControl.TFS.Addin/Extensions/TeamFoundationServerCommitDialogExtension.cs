// TeamFoundationServerCommitDialogExtension.cs
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
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Extensions
{
    public class TeamFoundationServerCommitDialogExtension : CommitDialogExtension
    {
        Gtk.HBox _content;
        Gtk.TreeView _workItemsView;
        Gtk.ListStore _workItemStore;
        Gtk.ListStore _checkinActions;

        public Dictionary<int, WorkItemCheckinAction> WorkItems
        {
            get
            {
                var workItems = new Dictionary<int, WorkItemCheckinAction>();
                Gtk.TreeIter iter;
               
                if (_workItemStore.GetIterFirst(out iter))
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

        public override bool Initialize(ChangeSet changeSet)
        {
            var repo = changeSet.Repository as TeamFoundationServerRepository;

            if (repo != null)
            {
                this.Add(_content);
                _content.Show();
                this.Show();

                return true;
            }

            return false;
        }

        public override bool OnBeginCommit(ChangeSet changeSet)
        {
            changeSet.ExtendedProperties["TFS.WorkItems"] = WorkItems;
            return true;
        }

        public override void OnEndCommit(ChangeSet changeSet, bool success)
        {
            base.OnEndCommit(changeSet, success);
        }

        void Init()
        {
            _content = new Gtk.HBox();
            _workItemsView = new Gtk.TreeView();
            _workItemStore = new Gtk.ListStore(typeof(int), typeof(string), typeof(string));
            _checkinActions = new Gtk.ListStore(typeof(string));
        }

        void BuildGui()
        {
            Gtk.CellRendererText cellId = new Gtk.CellRendererText();
            Gtk.TreeViewColumn idColumn = new Gtk.TreeViewColumn();
            idColumn.Title = GettextCatalog.GetString("ID");
            idColumn.PackStart(cellId, false);
            idColumn.AddAttribute(cellId, "text", 0);

            Gtk.CellRendererText cellTitle = new Gtk.CellRendererText();
            Gtk.TreeViewColumn titleColumn = new Gtk.TreeViewColumn();
            titleColumn.Title = "Title";
            titleColumn.Expand = true;
            titleColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;
            titleColumn.PackStart(cellTitle, true);
            titleColumn.AddAttribute(cellTitle, "text", 1);

            Gtk.CellRendererCombo cellAction = new Gtk.CellRendererCombo();
            Gtk.TreeViewColumn actionColumn = new Gtk.TreeViewColumn();
            actionColumn.Title = "Action";
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

            _content.PackStart(_workItemsView, true, true, 3);

            Gtk.VButtonBox buttonBox = new Gtk.VButtonBox();
            Gtk.Button addButton = new Gtk.Button();
            addButton.Label = GettextCatalog.GetString("Add Work Item");
            addButton.Clicked += OnAddWorkItem;

            addButton.WidthRequest = 120;

            buttonBox.PackStart(addButton);
            buttonBox.Layout = Gtk.ButtonBoxStyle.Start;

            _content.PackStart(buttonBox, false, false, 3);

            ShowAll();
        }

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
                }
            }
        }

        void OnActionChanged(object o, Gtk.EditedArgs args)
        {
            Gtk.TreeSelection selection = _workItemsView.Selection;
            Gtk.TreeIter iter;
           
            if (!selection.GetSelected(out iter))
            {
                return;
            }

            _workItemStore.SetValue(iter, 2, args.NewText);
        }

        KeyValuePair<int, WorkItemCheckinAction> GetValue(Gtk.TreeIter iter)
        {
            var id = (int)_workItemStore.GetValue(iter, 0);
            var checkinAction = (WorkItemCheckinAction)Enum.Parse(typeof(WorkItemCheckinAction), (string)_workItemStore.GetValue(iter, 2));
           
            return new KeyValuePair<int, WorkItemCheckinAction>(id, checkinAction);
        }
    }
}