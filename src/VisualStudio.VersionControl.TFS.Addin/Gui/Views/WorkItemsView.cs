using System;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client.Metadata;
using Microsoft.TeamFoundation.WorkItemTracking.Client.Objects;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.ProgressMonitoring;
using static MonoDevelop.VersionControl.TFS.Gui.Pads.TeamExplorerPad;

namespace MonoDevelop.VersionControl.TFS.Gui.Views
{
    public class WorkItemsView : ViewContent
    {
        Gtk.VBox _view;
        Gtk.TreeView _treeView;
        Gtk.TreeStore _treeStore;
        Gtk.TreeView _listView;
        Gtk.TreeStore _listStore;

        #region Constructor

        public WorkItemsView(ProjectInfo project)
        {
            ContentName = GettextCatalog.GetString("Work Items: " + project.Name);

            Init();
            BuildGui();
            AttachEvents();

            using (var progress = new MessageDialogProgressMonitor(true, false, false))
            {
                progress.BeginTask("Loading...", 2);
                LoadWorkItems(project);
                progress.Step(1);
                progress.EndTask();
            }
        }

        #endregion

        public override Control Control => _view;

        public static void Show(ProjectInfo project)
        {
            var sourceControlExplorerView = new WorkItemsView(project);
            IdeApp.Workbench.OpenDocument(sourceControlExplorerView, true);
        }

        void Init()
        {
            _view = new Gtk.VBox();

            _treeView = new Gtk.TreeView();
            _treeStore = new Gtk.TreeStore(typeof(string), typeof(TeamExplorerNodeType), typeof(object));
     
            Gtk.TreeViewColumn treeColumn = new Gtk.TreeViewColumn();
            var textRenderer = new Gtk.CellRendererText();
            treeColumn.PackStart(textRenderer, true);
            treeColumn.SetAttributes(textRenderer, "text", 0);
            _treeView.AppendColumn(treeColumn);

            _listView = new Gtk.TreeView();
            _listView.Selection.Mode = Gtk.SelectionMode.Multiple;
            _listStore = new Gtk.TreeStore(typeof(string));
        }

        void BuildGui()
        {
            Gtk.HPaned mainBox = new Gtk.HPaned();

            Gtk.VBox treeViewBox = new Gtk.VBox();
            treeViewBox.WidthRequest = 300;
            treeViewBox.PackStart(_treeView, true, true, 0);
            mainBox.Pack1(treeViewBox, false, false);

            Gtk.VBox rightBox = new Gtk.VBox();
            rightBox.PackStart(_listView, true, true, 0);
            mainBox.Pack2(rightBox, true, true);

            _view.PackStart(mainBox, true, true, 0);
            _view.ShowAll();
        }

        void AttachEvents()
        {
            _treeView.Selection.Changed += OnWorkItemChanged;
            _treeView.RowActivated += OnTreeViewItemClicked;
        }

        void LoadWorkItems(ProjectInfo project)
        {
            _treeStore.Clear();

            var node = _treeStore.AppendNode();

            _treeStore.SetValue(node, 0, project.Name);
            _treeStore.SetValue(node, 1, TeamExplorerNodeType.Project);
            _treeStore.SetValue(node, 2, project);

            var workItemManager = new WorkItemManager(project.Collection);
            var workItemProject = workItemManager.GetByGuid(project.Guid);

            if (workItemProject != null)
            {
                var childNode = _treeStore.AppendNode(node);
                _treeStore.SetValue(childNode, 0, "Work Items");
                _treeStore.SetValue(childNode, 1, TeamExplorerNodeType.WorkItems);

                var privateQueries = workItemManager.GetMyQueries(workItemProject);
               
                if (privateQueries.Any())
                {
                    var privateChildNode = _treeStore.AppendNode(childNode);
                    _treeStore.SetValue(privateChildNode, 0, "My Queries");
                    _treeStore.SetValue(privateChildNode, 1, TeamExplorerNodeType.WorkItemQueryType);
                                      
                    foreach (var query in privateQueries)
                    {
                        var privateQueryChildNode = _treeStore.AppendNode(privateChildNode);
                        _treeStore.SetValue(privateQueryChildNode, 0, query.QueryName);
                        _treeStore.SetValue(privateQueryChildNode, 1, TeamExplorerNodeType.WorkItemQuery);
                        _treeStore.SetValue(privateQueryChildNode, 2, query);
                    }
                }

                var publicQueries = workItemManager.GetPublicQueries(workItemProject);
             
                if (publicQueries.Any())
                {
                    var publicChildNode = _treeStore.AppendNode(childNode);
                    _treeStore.SetValue(publicChildNode, 0, "Public");
                    _treeStore.SetValue(publicChildNode, 1, TeamExplorerNodeType.WorkItemQueryType);
                                 
                    foreach (var query in publicQueries)
                    {
                        var publicQueryChildNode = _treeStore.AppendNode(publicChildNode);
                        _treeStore.SetValue(publicQueryChildNode, 0, query.QueryName);
                        _treeStore.SetValue(publicQueryChildNode, 1, TeamExplorerNodeType.WorkItemQuery);
                        _treeStore.SetValue(publicQueryChildNode, 2, query);
                    }
                }
            }

            _treeView.Model = _treeStore;
        }

        void LoadQueries(StoredQuery query)
        {
            _listStore.Clear();

            using (var progress = new MessageDialogProgressMonitor(true, false, false))
            {
                var fields = CachedMetaData.Instance.Fields;
                WorkItemStore store = new WorkItemStore(query);
                var data = store.LoadByPage(progress);
             
                if (data.Count > 0)
                {
                    var firstItem = data[0];
                }
            }
        }

        void OnWorkItemChanged(object sender, EventArgs e)
        {
            Gtk.TreeIter iter;

            if (!_treeView.Selection.GetSelected(out iter))
                return;

            var item = _treeStore.GetValue(iter, 2) as StoredQuery;

            if(item != null)
            {
                LoadQueries(item);
            }
        }

        void OnTreeViewItemClicked(object o, Gtk.RowActivatedArgs args)
        {
            var isExpanded = _treeView.GetRowExpanded(args.Path);
            if (isExpanded)
                _treeView.CollapseRow(args.Path);
            else
                _treeView.ExpandRow(args.Path, false);
        }
    }
}