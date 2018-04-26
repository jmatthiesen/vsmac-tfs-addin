using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MonoDevelop.Components;
using MonoDevelop.Components.Docking;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.VersionControl.TFS.Extensions;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Gui.Views;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.VersionControl.TFS.Gui.Pads
{
    /// <summary>
    /// Team explorer pad.
    /// </summary>
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

		DockItemToolbar _toolbar;
        VBox _content;
        Button _addbutton;
        TreeView _treeView;
        TreeStore _treeStore;
        DataField<string> _name;
        DataField<TeamExplorerNodeType> _type;
        DataField<object> _item;
		Spinner _projectsSpinner;

		Task _worker;
        CancellationTokenSource _workerCancel;

		List<TeamFoundationServer> _servers;
		List<ProjectWorkItem> _projects;

        TeamFoundationServerVersionControlService _service;

        System.Action OnServersChanged;

        public override Control Control { get { return new XwtControl(_content); } }

        /// <summary>
		/// Initialize TeamExplorerPad.
        /// </summary>
        /// <param name="window">Window.</param>
        protected override async void Initialize(IPadWindow window)
        {
            base.Initialize(window);

			Init();
            AddButtons(window);
            AttachEvents();

			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor(true))
            {
				monitor.BeginTask(GettextCatalog.GetString(GettextCatalog.GetString("Loading...")), NumberProjects() + 3);
				ClearData();
				monitor.Step();
				Loading(true);
				monitor.Step();
				await UpdateDataAsync(monitor);
				Loading(false);
                monitor.EndTask();
            }
        }

        /// <summary>
        /// Releases all resource used by the <see cref="T:MonoDevelop.VersionControl.TFS.Gui.Pads.TeamExplorerPad"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the
        /// <see cref="T:MonoDevelop.VersionControl.TFS.Gui.Pads.TeamExplorerPad"/>. The <see cref="Dispose"/> method
        /// leaves the <see cref="T:MonoDevelop.VersionControl.TFS.Gui.Pads.TeamExplorerPad"/> in an unusable state.
        /// After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="T:MonoDevelop.VersionControl.TFS.Gui.Pads.TeamExplorerPad"/> so the garbage collector can reclaim
        /// the memory that the <see cref="T:MonoDevelop.VersionControl.TFS.Gui.Pads.TeamExplorerPad"/> was occupying.</remarks>
        public override void Dispose()
        {
            _treeView.Dispose();
            _treeStore.Dispose();
            _content.Dispose();
			_workerCancel?.Cancel();

            base.Dispose();
        } 

        /// <summary>
        /// Init and create the TeamExplorerPad User Interface.
        /// </summary>
        void Init()
		{
            _workerCancel = new CancellationTokenSource();

			_servers = new List<TeamFoundationServer>();
			_projects = new List<ProjectWorkItem>();

			_content = new VBox
			{
				BackgroundColor = Colors.White
			};

			_projectsSpinner = new Spinner
            {
                HeightRequest = 24,
                WidthRequest = 24,
                Animate = true,
                HorizontalPlacement = WidgetPlacement.Center,
                VerticalPlacement = WidgetPlacement.Center,
                Visible = false
            };

            _content.PackStart(_projectsSpinner, true, true);

			_treeView = new TreeView
			{
				BorderVisible = false,
				HeadersVisible = false
			};

			_name = new DataField<string>();
			_type = new DataField<TeamExplorerNodeType>();
			_item = new DataField<object>();
			_treeStore = new TreeStore(_name, _type, _item);

			_treeView.Columns.Add(new ListViewColumn(string.Empty, new TextCellView(_name)) { Expands = true });
			_treeView.DataSource = _treeStore;
			_content.PackStart(_treeView, true, true);

			_service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            OnServersChanged = UpdateData;
        }

        /// <summary>
        /// Add Pad action buttons
        /// </summary>
		/// <param name="window">IPadWindow.</param>
		void AddButtons(IPadWindow window)
        {
            _toolbar = window.GetToolbar(DockPositionType.Top);

			_addbutton = new Button(GettextCatalog.GetString("Add Server"))
			{
				Image = ImageService.GetIcon(Stock.Add)
			};

			_toolbar.Add(new XwtControl(_addbutton));

			_toolbar.ShowAll();
        }

        /// <summary>
        /// Register TeamExplorerPad events.
        /// </summary>
        void AttachEvents()
        {
            _addbutton.Clicked += OnConnectToServer;
            _treeView.RowActivated += OnRowClicked;
            _service.OnServersChange += OnServersChanged;
        }

        /// <summary>
        /// Return the numbers of projects.
        /// </summary>
        /// <returns>The total number of projects.</returns>
        int NumberProjects()
		{
			int projects = 0;

			var servers = _service.Servers;  

			foreach(var server in servers)
			{
				foreach(var projectCollection in server.ProjectCollections)
				{
					foreach(var project in projectCollection.Projects)
					{
						projects++;
					}
				}
			}

			return projects;
		}

        /// <summary>
        /// Reset the server and project lists.
        /// </summary>
        void ClearData()
		{
			_treeStore.Clear();

			_servers.Clear();
			_projects.Clear();
		}

		void Loading(bool isLoading)
		{
			_projectsSpinner.Visible = isLoading;
			_treeView.Visible = !isLoading;
			_addbutton.Sensitive = !isLoading;
		}

        async void UpdateData()
		{
            using (var monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor(true))
            {
				monitor.BeginTask(GettextCatalog.GetString(GettextCatalog.GetString("Loading...")), NumberProjects() + 3);
                ClearData();
                monitor.Step();
                Loading(true);
                monitor.Step();
				await UpdateDataAsync(monitor);
                Loading(false);
                monitor.EndTask();
            }
		}

        /// <summary>
		/// Updates the data (servers, projects) async.
        /// </summary>
        /// <returns>The data async.</returns>
        /// <param name="monitor">Monitor.</param>
		Task UpdateDataAsync(ProgressMonitor monitor)
        {         
			_worker = Task.Factory.StartNew(delegate
			{
				if (!_workerCancel.Token.IsCancellationRequested)
				{
					var servers = _service.Servers;

					foreach (var server in servers)
					{
						_servers.Add(server);

						foreach (ProjectCollection pc in server.ProjectCollections)
						{
							var workItemManager = new WorkItemManager(pc);

							foreach (ProjectInfo projectInfo in pc.Projects.OrderBy(x => x.Name))
							{
								if (!IsSourceControlGitEnabled(projectInfo.ProjectDetails))
								{
									var workItemProject = workItemManager.GetByGuid(projectInfo.Id);

									if (workItemProject != null)
									{
										_projects.Add(new ProjectWorkItem
										{
											Project = projectInfo,
											WorkItemProject = workItemProject
										});                                        
									}
								}

								monitor.Step();
							}
						}
					}
                       
                    // Updates the UI in the UI Thread
					Application.Invoke(() =>
					{
						foreach (var server in _servers)
						{
							var node = _treeStore.AddNode().SetValue(_name, server.Name)
												 .SetValue(_type, TeamExplorerNodeType.Server)
												 .SetValue(_item, server);                     
			
							foreach (ProjectCollection projectCollection in _projects
							         .Select(p => p.Project.Collection)
							         .Where(pc => pc.Server.Name == server.Name)
							         .DistinctBy(pc => pc.Id))
							{                        
								node.AddChild().SetValue(_name, projectCollection.Name)
									.SetValue(_type, TeamExplorerNodeType.ProjectCollection)
								    .SetValue(_item, projectCollection);
    
								foreach (ProjectWorkItem projectWorkItem in _projects
								         .Where(c => c.Project.Collection.Id == projectCollection.Id)
								         .DistinctBy(w => w.Project.Id)
								         .OrderBy(x => x.Project.Name))
								{
									node.AddChild().SetValue(_name, projectWorkItem.Project.Name)
										.SetValue(_type, TeamExplorerNodeType.Project)
									    .SetValue(_item, projectWorkItem.Project);

									var workItemProject = projectWorkItem.WorkItemProject;

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

						monitor.Step();

						ExpandTree();
					});
				}
			}, _workerCancel.Token, TaskCreationOptions.LongRunning);

			return _worker;
        }

		bool IsSourceControlGitEnabled(ProjectDetails projectDetails)
        {
			if(projectDetails == null)
			{
				return false;
			}

            if (projectDetails.Details.Any(p => p.Name == "System.SourceControlGitEnabled"))
            {
                return true;
            }

            return false;
        }

        void ExpandTree()
        {
			Application.Invoke(() =>
			{
				var servers = _service.Servers;
			
				if (servers.Count > 1)
				{
					_treeView.ExpandAll();
				}
				else
				{
					var node = _treeStore.GetFirstNode();

					if (node.CurrentPosition == null)
						return;

					_treeView.ExpandRow(node.CurrentPosition, true);

					while (node.MoveNext())
					{
						_treeView.ExpandRow(node.CurrentPosition, false);
					}
				}
			});
        }

        /// <summary>
        /// Fires when a row is clicked.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        void OnRowClicked(object sender, TreeViewRowEventArgs e)
        {
            var node = _treeStore.GetNavigatorAt(e.Position);
            var nodeType = node.GetValue(_type);

            if(nodeType == TeamExplorerNodeType.SourceControl)
            {
                node.MoveToParent();
                var project = (ProjectInfo)node.GetValue(_item);
		  
				// Open SourceControlExplorerView
				SourceControlExplorerView.Show(project);  
            }

            if (nodeType == TeamExplorerNodeType.WorkItems)
            {
                node.MoveToParent();
                var project = (ProjectInfo)node.GetValue(_item);

				// Open WorkItemsView
				WorkItemsView.Show(project); 
            }
        }

        /// <summary>
        /// Ons the connect to server.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        void OnConnectToServer(object sender, EventArgs e)
        {
			using (var dialog = new ConnectToServerDialog())
            {
                dialog.Run(MessageDialog.RootWindow);
            }
        }
    }
}