// ChooseWorkItemDialog.cs
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
using System.Linq;
using Autofac;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class ChooseWorkItemDialog : Dialog
    {       
        TreeView _queryView;
        DataField<string> _titleField;
        DataField<StoredQuery> _queryField;
        DataField<ProjectCollection> _collectionField;
        TreeStore _queryStore;
        TreeView _listView;
        TreeStore _listStore;
        DataField<WorkItem> _workItemField;

        TeamFoundationServerVersionControlService _versionControlService;
        WorkItem _workItem;

        public ChooseWorkItemDialog()
        {
            Init();
            BuildGui();
            AttachEvents();
        }

        public WorkItem WorkItem
        {
            get { return _workItem;  }
            private set { _workItem = value; }
        }

        void Init()
        {
            _queryView = new TreeView();
            _titleField = new DataField<string>();
            _queryField = new DataField<StoredQuery>();
            _collectionField = new DataField<ProjectCollection>(); 
            _queryStore = new TreeStore(_titleField, _queryField, _collectionField);
            _listView = new TreeView();

            _versionControlService = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();         
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Choose Work Item");
       
            VBox content = new VBox();
            HBox mainBox = new HBox();
            _queryView.Columns.Add(new ListViewColumn(string.Empty, new TextCellView(_titleField)));
            _queryView.DataSource = _queryStore;
            _queryView.WidthRequest = 200;

            CreateQueries();

            mainBox.PackStart(_queryView);

            _listView.WidthRequest = 400;
            _listView.HeightRequest = 400;

            mainBox.PackStart(_listView, true, true);

            content.PackStart(mainBox, true, true);

            HBox buttonBox = new HBox();

            Button okButton = new Button(GettextCatalog.GetString("Ok"));
            okButton.WidthRequest = GuiSettings.ButtonWidth;
            okButton.Clicked += (sender, e) => Respond(Command.Ok);
            buttonBox.PackEnd(okButton);

            content.PackStart(buttonBox);
            Content = content;
        }

        void AttachEvents()
        {
            _queryView.RowActivated += (sender, e) =>
            {
                var navigator = _queryStore.GetNavigatorAt(e.Position);
                var query = navigator.GetValue(_queryField);

                if (query != null)
                {
                    var collection = navigator.GetValue(_collectionField);
                    LoadQuery(query, collection);
                }
            };

            _listView.SelectionChanged += (sender, e) =>
            {
                var position = _listView.SelectedRow;
                var navigator = _listStore.GetNavigatorAt(position);
                var workItem = navigator.GetValue(_workItemField);

                if (workItem != null)
                {
                    WorkItem = workItem;
                }
            };
        }

        void CreateQueries()
        {
            _queryStore.Clear();
           
            foreach (var server in _versionControlService.Servers)
            {
                var node = _queryStore.AddNode().SetValue(_titleField, server.Name);
              
                foreach (var projectCollection in server.ProjectCollections)
                {
                    node.AddChild().SetValue(_titleField, projectCollection.Name);
                    var workItemManager = new WorkItemManager(projectCollection);
                 
                    foreach (var projectInfo in projectCollection.Projects.OrderBy(x => x.Name))
                    {
                        var workItemProject = workItemManager.GetByGuid(projectInfo.Id);
                      
                        if (workItemProject == null)
                            continue;

                        node.AddChild().SetValue(_titleField, projectInfo.Name);

                        var privateQueries = workItemManager.GetMyQueries(workItemProject);
                     
                        if (privateQueries.Any())
                        {
                            node.AddChild().SetValue(_titleField, "My Queries");
                            foreach (var query in privateQueries)
                            {
                                node.AddChild().SetValue(_titleField, query.QueryName).SetValue(_queryField, query).SetValue(_collectionField, projectCollection);
                                node.MoveToParent();
                            }
                            node.MoveToParent();
                        }

                        var publicQueries = workItemManager.GetPublicQueries(workItemProject);
                       
                        if (publicQueries.Any())
                        {
                            node.AddChild().SetValue(_titleField, "Public");
                           
                            foreach (var query in publicQueries)
                            {
                                node.AddChild().SetValue(_titleField, query.QueryName).SetValue(_queryField, query).SetValue(_collectionField, projectCollection);
                                node.MoveToParent();
                            }

                            node.MoveToParent();
                        }
                        node.MoveToParent();
                    }

                    _queryView.ExpandRow(node.CurrentPosition, true);
                }
            }

            var cursor = _queryStore.GetFirstNode();
          
            if (cursor.MoveToChild()) // Project Collections
                _queryView.ExpandToRow(cursor.CurrentPosition);
        }

        void LoadQuery(StoredQuery query, ProjectCollection collection)
        {
            _listView.Columns.Clear();

            using (var progress = new Ide.ProgressMonitoring.MessageDialogProgressMonitor(true, false, false))
            {
                var fields = CachedMetaData.Instance.Fields;
                WorkItemStore store = new WorkItemStore(query, collection);
                var data = store.LoadByPage(progress);
               
                if (data.Count > 0)
                {
                    var firstItem = data[0];
                    List<IDataField> dataFields = new List<IDataField>();
                    var mapping = new Dictionary<Field, IDataField<object>>();

                    foreach (var item in firstItem.WorkItemInfo.Keys)
                    {
                        var field = fields[item];
                        var dataField = new DataField<object>();
                        dataFields.Add(dataField);
                        mapping.Add(field, dataField);
                    }

                    if (dataFields.Any())
                    {
                        _workItemField = new DataField<WorkItem>();
                        dataFields.Insert(0, _workItemField);
                        _listStore = new TreeStore(dataFields.ToArray());

                        foreach (var map in mapping)
                        {
                            _listView.Columns.Add(map.Key.Name, map.Value);
                        }

                        _listView.DataSource = _listStore;

                        foreach (var workItem in data)
                        {
                            var row = _listStore.AddNode();
                            row.SetValue(_workItemField, workItem);
                           
                            foreach (var map in mapping)
                            {
                                object value;

                                if (workItem.WorkItemInfo.TryGetValue(map.Key.ReferenceName, out value))
                                {
                                    row.SetValue(map.Value, value);
                                }
                                else
                                {
                                    row.SetValue(map.Value, null);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}