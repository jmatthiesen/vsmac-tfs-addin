using System;
using System.Linq;
using Autofac;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Gui.Views;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Pads
{
    public class TeamExplorerPad : PadContent
    {
        public enum TeamExplorerNodeType
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

        TeamFoundationServerVersionControlService _service;

        System.Action OnServersChanged;

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
     
            _service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            OnServersChanged = UpdateData;
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
            _treeView.RowActivated += OnRowClicked;

            _service.OnServersChange += OnServersChanged;
        }

        void UpdateData()
        {
            _treeStore.Clear();

            var servers = _service.Servers;

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

                    var workItemManager = new WorkItemManager(pc);

                    foreach (ProjectInfo projectInfo in pc.Projects.OrderBy(x => x.Name))
                    {
                        node.AddChild().SetValue(_name, projectInfo.Name)
                            .SetValue(_type, TeamExplorerNodeType.Project)
                            .SetValue(_item, projectInfo);

                        var workItemProject = workItemManager.GetByGuid(projectInfo.Id);
                    
                        if (workItemProject != null)
                        {
                            node.AddChild()
                                .SetValue(_name, "Work Items")
                                .SetValue(_type, TeamExplorerNodeType.WorkItems);
                        
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
                SourceControlExplorerView.Show(project);
            }

            if (nodeType == TeamExplorerNodeType.WorkItems)
            {
                node.MoveToParent();
                var project = (ProjectInfo)node.GetValue(_item);
                WorkItemsView.Show(project);
            }
        }

        void OnConnectToServer(object sender, EventArgs e)
        {
            var connectToServerDialog = new ConnectToServerDialog();
            connectToServerDialog.Run();
        }
    }
}