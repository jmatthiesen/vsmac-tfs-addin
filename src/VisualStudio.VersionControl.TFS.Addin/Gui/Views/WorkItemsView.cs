// WorkItemsView.cs
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Gui;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;
using Xwt.Drawing;
using static MonoDevelop.VersionControl.TFS.Gui.Pads.TeamExplorerPad;

namespace MonoDevelop.VersionControl.TFS.Gui.Views
{
	/// <summary>
    /// Work items view.
    /// </summary>
	public class WorkItemsView : ViewContent
    {
		XwtControl _control;
        VBox _view;
		Button _refreshButton;
        TreeView _treeView;
		DataField<string> _nameField;
		DataField<TeamExplorerNodeType> _nodeTypeField;
		DataField<object> _objectField;
        TreeStore _treeStore;
        TreeView _listView;
        DataField<WorkItem> _workItemField;
        TreeStore _listStore;
		Label _noResultsLabel;

		ProjectInfo _project;
        ProjectCollection _projectCollection;

        #region Constructor

        internal WorkItemsView(ProjectInfo project)
        {
            ContentName = GettextCatalog.GetString("Work Items: " + project.Name);

            Init(project);
            BuildGui();
            AttachEvents();

			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor(true))
            {
				monitor.BeginTask(GettextCatalog.GetString("Loading..."), 2);
                LoadWorkItems(project);
				monitor.Step(1);
                ExpandAllWorkItems();
				monitor.EndTask();
            }
        }

        #endregion

		public override Control Control => _control;

        internal static void Show(ProjectInfo project)
        {
            var sourceControlExplorerView = new WorkItemsView(project);
            IdeApp.Workbench.OpenDocument(sourceControlExplorerView, true);
        }
        
        /// <summary>
		/// Init WorkItemsView.
        /// </summary>
        /// <param name="project">Project.</param>
        void Init(ProjectInfo project)
        {
			_project = project;
            _projectCollection = project.Collection;
                     
            _view = new VBox();
			_control = new XwtControl(_view);

            _refreshButton = new Button(GettextCatalog.GetString("Refresh"))
            {
                Image = ImageService.GetIcon(Stock.StatusWorking),
				WidthRequest = GuiSettings.ButtonWidth,
				Margin = new WidgetSpacing(6, 0, 0, 0)
            };

			_treeView = new TreeView
			{
				BorderVisible = false,
				MinWidth = 200,
                WidthRequest = 300
            };

			_nameField = new DataField<string>();
			_nodeTypeField = new DataField<TeamExplorerNodeType>();
			_objectField = new DataField<object>();
			_treeStore = new TreeStore(_nameField, _nodeTypeField, _objectField);
     
			_treeView.Columns.Add("Name", _nameField);

			_listView = new TreeView
            {
                BorderVisible = false,
				UseAlternatingRowColors = true,
				GridLinesVisible = GridLines.None,
				ExpandHorizontal = true,
				MinWidth = 500
            };

            _workItemField = new DataField<WorkItem>();

			_noResultsLabel = new Label(GettextCatalog.GetString("No Results"))
			{
				VerticalPlacement = WidgetPlacement.Start,
				HorizontalPlacement = WidgetPlacement.Start,
				MinWidth = 400,
				Margin = new WidgetSpacing(12),
				Visible = false
			};
		}

        /// <summary>
		/// Builds the WorkItemsView GUI.
        /// </summary>
        void BuildGui()
        {
			HBox headerBox = new HBox();
			headerBox.PackStart(_refreshButton, false, false);
            _view.PackStart(headerBox, false, false);
            
            HPaned mainBox = new HPaned();
         
			VBox treeViewBox = new VBox
			{
				WidthRequest = 300
			};

			treeViewBox.PackStart(_treeView, true, true);
			mainBox.Panel1.Content = treeViewBox;

			VBox rightBox = new VBox
			{
				BackgroundColor = Colors.White,
				ExpandHorizontal = true
			};

			rightBox.PackStart(_noResultsLabel, false, false);
			rightBox.PackEnd(_listView, true, true);
			mainBox.Panel2.Content = rightBox;

            _view.PackStart(mainBox, true, true);
        }

        /// <summary>
        /// Attachs the events.
        /// </summary>
        void AttachEvents()
        {
			_refreshButton.Clicked += OnRefresh;
			_listView.KeyPressed += OnWorkItemKeyPressed;
			_listView.ButtonPressed += OnListViewMouseClick;
			_treeView.SelectionChanged += OnWorkItemChanged;
            _treeView.RowActivated += OnTreeViewItemClicked;
        }

		void OnRefresh(object sender, EventArgs e)
		{
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor(true))
            {
				monitor.BeginTask(GettextCatalog.GetString("Loading..."), 2);
                LoadWorkItems(_project);
				monitor.Step(1);
                ExpandAllWorkItems();
				monitor.EndTask();
            }
		}

		void OnWorkItemKeyPressed(object sender, KeyEventArgs e)
        {
			if (e.Modifiers == ModifierKeys.Command && (e.Key == Key.c || e.Key == Key.C))
            {
				CopyWorkItemToClipboard();
            }
        }
        
        /// <summary>
        /// Copies the selected WorkItem to clipboard.
        /// </summary>
		void CopyWorkItemToClipboard()
        {
            var store = (TreeStore)_listView.DataSource;
            StringBuilder builder = new StringBuilder();
         
			foreach (var row in _listView.SelectedRows)
            {
                List<string> rowValues = new List<string>();
            
				foreach (var column in _listView.Columns)
                {
                    var field = ((TextCellView)column.Views[0]).TextField as IDataField<object>;
                    var val = Convert.ToString(store.GetNavigatorAt(row).GetValue(field));
                    rowValues.Add(val);
                }

                builder.AppendLine(string.Join("\t", rowValues));
            }

            Clipboard.SetText(builder.ToString());
        }

        /// <summary>
        /// Opens the WorkItem URL.
        /// </summary>
        void OpenUrl()
		{
			try
			{
				var store = (TreeStore)_listView.DataSource;

				foreach (var row in _listView.SelectedRows)
				{
					var node = _listStore.GetNavigatorAt(row);
					var workItem = node.GetValue(_workItemField);

					if (workItem != null)
					{
						// More information: https://docs.microsoft.com/en-us/vsts/work/work-items/work-item-url-hyperlink?view=vsts
						var workItemUrl = string.Format("{0}DefaultCollection/{1}/_workitems?id={2}&_a=edit",
														_projectCollection.Server.Uri.OriginalString,
														_project.Name,
														workItem.Id);

						Process.Start(workItemUrl);
					}
				}
			}
			catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageService.ShowError(GettextCatalog.GetString("Cannot open WorkItem URL. Please, try again."));
            }
		}

        /// <summary>
        /// Loads the work items.
        /// </summary>
        /// <param name="project">Project.</param>
        void LoadWorkItems(ProjectInfo project)
        {
            _treeStore.Clear();

			var node = _treeStore.AddNode();
            
			node.SetValue(_nameField, project.Name);
			node.SetValue(_nodeTypeField, TeamExplorerNodeType.Project);
			node.SetValue(_objectField, project);

            var workItemManager = new WorkItemManager(project.Collection);
            var workItemProject = workItemManager.GetByGuid(project.Id);

            if (workItemProject != null)
            {
				var childNode = _treeStore.AddNode(node.CurrentPosition);

				childNode.SetValue(_nameField, "Work Items");
				childNode.SetValue(_nodeTypeField, TeamExplorerNodeType.WorkItems);

                var privateQueries = workItemManager.GetMyQueries(workItemProject);
               
                if (privateQueries.Any())
                {
					var privateChildNode = _treeStore.AddNode(childNode.CurrentPosition);
					privateChildNode.SetValue(_nameField, "My Queries");
					privateChildNode.SetValue(_nodeTypeField, TeamExplorerNodeType.WorkItemQueryType);
                                      
                    foreach (var query in privateQueries)
                    {
						var privateQueryChildNode = _treeStore.AddNode(privateChildNode.CurrentPosition);
						privateQueryChildNode.SetValue(_nameField, query.QueryName);
						privateQueryChildNode.SetValue(_nodeTypeField, TeamExplorerNodeType.WorkItemQuery);
						privateQueryChildNode.SetValue(_objectField, query);
                    }
                }

                var publicQueries = workItemManager.GetPublicQueries(workItemProject);
             
                if (publicQueries.Any())
                {
					var publicChildNode = _treeStore.AddNode(childNode.CurrentPosition);
					publicChildNode.SetValue(_nameField, "Public");
					publicChildNode.SetValue(_nodeTypeField, TeamExplorerNodeType.WorkItemQueryType);
                                 
                    foreach (var query in publicQueries)
                    {
						var publicQueryChildNode = _treeStore.AddNode(publicChildNode.CurrentPosition);
						publicQueryChildNode.SetValue(_nameField, query.QueryName);
						publicQueryChildNode.SetValue(_nodeTypeField, TeamExplorerNodeType.WorkItemQuery);
						publicQueryChildNode.SetValue(_objectField, query);
                    }
                }
            }

			_treeView.DataSource = _treeStore;
        }

        /// <summary>
        /// Expands all work items rows.
        /// </summary>
        void ExpandAllWorkItems()
        {
            _treeView.ExpandAll();
        }

        /// <summary>
        /// Loads the queries.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <param name="collection">Collection.</param>
        void LoadQueries(StoredQuery query, ProjectCollection collection)
        {
            _listView.Columns.Clear();

			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor(true))
			{
				if (!monitor.CancellationToken.IsCancellationRequested)
				{               
					var fields = CachedMetaData.Instance.Fields;
					WorkItemStore store = new WorkItemStore(query, collection);
					var data = store.LoadByPage(monitor);

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
									if (workItem.WorkItemInfo.TryGetValue(map.Key.ReferenceName, out object value))
									{
										row.SetValue(map.Value, value);
									}
									else
									{
										row.SetValue(map.Value, null);
									}
								}
							}

							_noResultsLabel.Visible = false;
							_listView.Visible = true;
						}
					}
					else
					{
						_noResultsLabel.Visible = true;
						_listView.Visible = false;
					}
				}
            }
        }

        void OnWorkItemChanged(object sender, EventArgs e)
		{
			if (_treeView.SelectedRow == null)
                return;

			var node = _treeStore.GetNavigatorAt(_treeView.SelectedRow);

			if (node.GetValue(_objectField) is StoredQuery item)
			{
				LoadQueries(item, _projectCollection);
			}
		}

		void OnTreeViewItemClicked(object o, TreeViewRowEventArgs args)
        {
			var rowPosition = args.Position;

			var isExpanded = _treeView.IsRowExpanded(rowPosition);

			if (isExpanded)
			{
				_treeView.CollapseRow(rowPosition);
			}
			else
			{
				_treeView.ExpandRow(rowPosition, false);
			}
        }

		[GLib.ConnectBefore]
		void OnListViewMouseClick(object o, ButtonEventArgs args)
        {
			if (args.Button == PointerButton.Right && _treeView.SelectedRows.Any())
            {
                var menu = BuildTreeViewPopupMenu();

                if (menu.Items.Count > 0)
                    menu.Popup();

                args.Handled = true;
            }
        }

        /// <summary>
        /// Builds the tree view popup menu.
        /// </summary>
        /// <returns>The tree view popup menu.</returns>
		Menu BuildTreeViewPopupMenu()
		{
			Menu menu = new Menu();

			var items = new List<WorkItem>();

			foreach (var path in _listView.SelectedRows)
            {
				var node = _listStore.GetNavigatorAt(path);
				var workItem = node.GetValue(_workItemField);
				items.Add(workItem);
            }

			menu.Items.Add(OpenUrlMenuItem(items));
			menu.Items.Add(CopyMenuItem(items));
                       
			return menu;
		}
        
        /// <summary>
        /// Copy WorkItem information to the Clipboard.
        /// </summary>
        /// <returns>The menu item.</returns>
        /// <param name="items">Items.</param>
		MenuItem CopyMenuItem(List<WorkItem> items)
        {
            MenuItem copy = new MenuItem(GettextCatalog.GetString("Copy to Clipboard"));

			copy.Clicked += (sender, e) =>
            {
				if(items.Any())
				{
					CopyWorkItemToClipboard();
				}
            };

			return copy;
        }

        /// <summary>
        /// Open URL menu item.
        /// </summary>
        /// <returns>The URL menu item.</returns>
        /// <param name="items">Items.</param>
		MenuItem OpenUrlMenuItem(List<WorkItem> items)
        {
            MenuItem copy = new MenuItem(GettextCatalog.GetString("Open Url"));

            copy.Clicked += (sender, e) =>
            {
                if (items.Any())
                {
                    OpenUrl();
                }
            };

            return copy;
        }
    }
}