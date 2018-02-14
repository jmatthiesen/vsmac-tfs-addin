using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client.Metadata;
using Microsoft.TeamFoundation.WorkItemTracking.Client.Objects;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.ProgressMonitoring;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Views
{
    public class WorkItemsView : ViewContent
    {
        StoredQuery _query;
        
        VBox _view;
        TreeView _listView;
        DataField<WorkItem> _workItemField;

        public WorkItemsView(StoredQuery query)
        {
            ContentName = GettextCatalog.GetString("Work Items");
         
            Init();
            BuildGui();
            LoadQuery(query);
        }

        public override Control Control => new XwtControl(_view);

        void Init()
        {
            _view = new VBox();
            _listView = new TreeView();
            _listView.SelectionMode = SelectionMode.Multiple;
            _workItemField = new DataField<WorkItem>();
        }

        public static void Show(StoredQuery query)
        {
            var workItemsView = new WorkItemsView(query);
            IdeApp.Workbench.OpenDocument(workItemsView, true);
        }

        void BuildGui()
        {
            _view.PackStart(_listView, true, true);
        }

        void LoadQuery(StoredQuery query)
        {
            _query = query;

            ContentName = GettextCatalog.GetString("Work Items: " + query.QueryName);

            _listView.Columns.Clear();

            using (var progress = new MessageDialogProgressMonitor(true, false, false))
            {
                var fields = CachedMetaData.Instance.Fields;
                WorkItemStore store = new WorkItemStore(query);
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
                        dataFields.Insert(0, _workItemField);
                        var listStore = new TreeStore(dataFields.ToArray());
                       
                        foreach (var map in mapping)
                        {
                            _listView.Columns.Add(map.Key.Name, map.Value);
                        }

                        _listView.DataSource = listStore;

                        foreach (var workItem in data)
                        {
                            var row = listStore.AddNode();
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