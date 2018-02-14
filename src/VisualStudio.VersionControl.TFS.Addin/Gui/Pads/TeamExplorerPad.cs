using System;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client.Objects;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs;
using VisualStudio.VersionControl.TFS.Addin.Gui.Views;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Pads
{
    public class TeamExplorerPad : PadContent
    {
        enum TeamExplorerNodeType
        {
            Server,
            ProjectCollection,
            Project,
            SourceControl,
            WorkItems,
            WorkItemQueryType,
            WorkItemQuery
        }

        VBox _content;
        Button _addbutton;
        TreeView _treeView;
        TreeStore _treeStore;
        DataField<string> _name;
        DataField<TeamExplorerNodeType> _type;
        DataField<object> _item;

        public override Control Control { get { return new XwtControl(_content); } }

        protected override void Initialize(IPadWindow window)
        {
            base.Initialize(window);

            Init();
            AddButtons(window);
            AttachEvents();
            UpdateData();
        }

        public override void Dispose()
        {
            _treeView.Dispose();
            _treeStore.Dispose();
            _content.Dispose();

            base.Dispose();
        }

        void Init()
        {
            _content = new VBox();
            _treeView = new TreeView();

            _name = new DataField<string>();
            _type = new DataField<TeamExplorerNodeType>();
            _item = new DataField<object>();
            _treeStore = new TreeStore(_name, _type, _item);

            _treeView.Columns.Add(new ListViewColumn(string.Empty, new TextCellView(_name)));
            _treeView.DataSource = _treeStore;
            _content.PackStart(_treeView, true, true);

            _treeView.RowActivated += OnRowClicked;
        }

        void AddButtons(IPadWindow window)
        {
            DockItemToolbar toolbar = window.GetToolbar(DockPositionType.Top);
            _addbutton = new Button(GettextCatalog.GetString("Add"));
            toolbar.Add(new XwtControl(_addbutton)); 
        }

        void AttachEvents()
        {
            _addbutton.Clicked += OnConnectToServer;
        }

        void UpdateData()
        {
            _treeStore.Clear();

            var servers = TeamFoundationServerClient.Settings.GetServers();

            foreach (var server in servers)
            {
                var node = _treeStore.AddNode().SetValue(_name, server.Name)
                                     .SetValue(_type, TeamExplorerNodeType.Server)
                                     .SetValue(_item, server);

                foreach (ProjectCollection pc in server.ProjectCollections)
                {
                    node.AddChild().SetValue(_name, pc.Name)
                        .SetValue(_type, TeamExplorerNodeType.ProjectCollection)
                        .SetValue(_item, pc);
                    
                    foreach (ProjectInfo projectInfo in pc.Projects.OrderBy(x => x.Name))
                    {
                        node.AddChild().SetValue(_name, projectInfo.Name)
                            .SetValue(_type, TeamExplorerNodeType.Project)
                            .SetValue(_item, projectInfo);

                        var workItemManager = new WorkItemManager(pc);
                        var workItemProject = workItemManager.GetByGuid(projectInfo.Guid);

                        if (workItemProject != null)
                        {
                            node.AddChild().SetValue(_name, "Work Items").SetValue(_type, TeamExplorerNodeType.WorkItems);
                            var privateQueries = workItemManager.GetMyQueries(workItemProject);
                           
                            if (privateQueries.Any())
                            {
                                node.AddChild().SetValue(_name, "My Queries").SetValue(_type, TeamExplorerNodeType.WorkItemQueryType);
                              
                                foreach (var query in privateQueries)
                                {
                                    node.AddChild().SetValue(_name, query.QueryName).SetValue(_type, TeamExplorerNodeType.WorkItemQuery).SetValue(_item, query);
                                    node.MoveToParent();
                                }

                                node.MoveToParent();
                            }

                            var publicQueries = workItemManager.GetPublicQueries(workItemProject);
                          
                            if (publicQueries.Any())
                            {
                                node.AddChild().SetValue(_name, "Public").SetValue(_type, TeamExplorerNodeType.WorkItemQueryType);
                              
                                foreach (var query in publicQueries)
                                {
                                    node.AddChild().SetValue(_name, query.QueryName).SetValue(_type, TeamExplorerNodeType.WorkItemQuery).SetValue(_item, query);
                                    node.MoveToParent();
                                }

                                node.MoveToParent();
                            }

                            node.MoveToParent();
                        }

                        node.AddChild()
                            .SetValue(_name, "Source Control")
                            .SetValue(_type, TeamExplorerNodeType.SourceControl);

                        node.MoveToParent();

                        node.MoveToParent();
                    }

                    node.MoveToParent();
                }
            }

            ExpandTree();
        }

        void ExpandTree()
        {
            var node = _treeStore.GetFirstNode();

            if (node.CurrentPosition == null)
                return;
            
            _treeView.ExpandRow(node.CurrentPosition, false);

            while (node.MoveNext())
            {
                _treeView.ExpandRow(node.CurrentPosition, false);
            }
        }

        void OnRowClicked(object sender, TreeViewRowEventArgs e)
        {
            var node = _treeStore.GetNavigatorAt(e.Position);
            var nodeType = node.GetValue(_type);

            if(nodeType == TeamExplorerNodeType.SourceControl)
            {
                node.MoveToParent();
                var project = (ProjectInfo)node.GetValue(_item);
                SourceControlExplorerView.Show(project.Collection);
            }

            if (nodeType == TeamExplorerNodeType.WorkItemQuery)
            {
                var query = (StoredQuery)node.GetValue(_item);
                WorkItemsView.Show(query);
            }
        }

        void OnConnectToServer(object sender, EventArgs e)
        {
            var dialog = new AddServerDialog();
            dialog.Run();
        }
    }
}