// SourceControlExplorerView.cs
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Views
{
	public class SourceControlExplorerView : ViewContent
    {
        #region Variables

        int _treeLevel;
        ProjectCollection _projectCollection;
        List<WorkspaceData> _workspaces;
        IWorkspaceService _currentWorkspace;

		XwtControl _control;
        VBox _view;
		HBox _headerBox;
		Button _addItemButton;
		Button _refreshButton;
        ComboBox _workspaceComboBox;
		Button _manageButton;
		DataField<string> _workspaceNameField;
		DataField<string> _workspacePathField;
		DataField<object> _workspaceObjectField;
		ListStore _workspaceStore;
        Label _workspaceLabel;
        Label _noWorkspacesLabel;
        TreeView _foldersView;
		DataField<BaseItem> _baseItemField;
		DataField<Xwt.Drawing.Image> _iconField;
        DataField<string> _nameField;
		TreeStore _foldersStore;
		List<TreeNavigator> _foldersList;
        Label _localFolder;
        TreeView _folderDetailsView;
		DataField<ExtendedItem> _extendedItemField;
		DataField<Xwt.Drawing.Image> _itemIconField;
		DataField<string> _itemNameField;
        DataField<string> _pendingField;
		DataField<string> _userField;
		DataField<string> _latestField;
		DataField<string> _lastCheckInField;
		TreeStore _folderDetailsStore;

		Task _worker;
        CancellationTokenSource _workerCancel;

        TeamFoundationServerVersionControlService _versionControlService;

        #endregion

        #region Constructor

        internal SourceControlExplorerView(ProjectCollection projectCollection)
        {
            _projectCollection = projectCollection;

            ContentName = GettextCatalog.GetString("Source Explorer") + " - " + _projectCollection.Server.Name + " - " + _projectCollection.Name;

            Init();
            BuildGui();
            AttachEvents();

			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor(true))
            {
				monitor.BeginTask(GettextCatalog.GetString(GettextCatalog.GetString("Loading...")), 2);
				LoadWorkspaces();
				monitor.Step(1);
				LoadFolders();
				ExpandPath(RepositoryPath.RootPath);
				monitor.EndTask();
			}
        }

		#endregion

		#region Properties

		public override Control Control => _control;

        #endregion

        #region Public Methods

        internal static void Show(ProjectCollection projectCollection)
        {
            var sourceControlExplorerView = new SourceControlExplorerView(projectCollection);
            IdeApp.Workbench.OpenDocument(sourceControlExplorerView, true);
        }

        internal static void Show(ProjectInfo projectInfo)
        {  
            var collection = projectInfo.Collection;
            var path = new RepositoryPath(RepositoryPath.RootPath + projectInfo.Name, true);
          
            var sourceControlExplorerView = new SourceControlExplorerView(collection);
            sourceControlExplorerView.ExpandPath(path);
            IdeApp.Workbench.OpenDocument(sourceControlExplorerView, true);
        }

        internal static void Show(ProjectCollection collection, string path, string fileName)
        {
            var sourceControlExplorerView = new SourceControlExplorerView(collection);
            sourceControlExplorerView.ExpandPath(path);
            sourceControlExplorerView.FindItem(fileName);
            IdeApp.Workbench.OpenDocument(sourceControlExplorerView, true);
        }

		#endregion

		#region Private Methods

		public override void Dispose()
		{
			_workerCancel?.Cancel();

			base.Dispose();
		}

		void Init()
        {
			_workerCancel = new CancellationTokenSource();

            _workspaces = new List<WorkspaceData>();
			_foldersList = new List<TreeNavigator>();

            _view = new VBox();
			_control = new XwtControl(_view);

			_headerBox = new HBox();

            _localFolder = new Label();
                       
			_addItemButton = new Button(GettextCatalog.GetString("Add new item"))
            {
				Style = ButtonStyle.Flat,
				Image = ImageService.GetIcon(Stock.NewDocumentIcon),
				Margin = new WidgetSpacing(6, 0, 0, 0)
            };

			_refreshButton = new Button(GettextCatalog.GetString("Refresh"))
            {
				Style = ButtonStyle.Flat,
                Image = ImageService.GetIcon(Stock.StatusWorking),
				WidthRequest = GuiSettings.ButtonWidth
            };

            _manageButton = new Button(GettextCatalog.GetString("Manage Workspaces"))
			{
                Style = ButtonStyle.Flat,
                Image = ImageService.GetIcon(Stock.Workspace)
            };
           
			_workspaceComboBox = new ComboBox
			{
				MinWidth = 300
			};
                     
			_workspaceNameField = new DataField<string>();
			_workspacePathField = new DataField<string>();
			_workspaceObjectField = new DataField<object>();

			_workspaceStore = new ListStore(_workspaceNameField, _workspacePathField, _workspaceObjectField);
			_workspaceComboBox.ItemsSource = _workspaceStore;

			_workspaceLabel = new Label(GettextCatalog.GetString("Workspace") + ":")
			{
				Margin = new WidgetSpacing(6, 0, 0, 0)
			};

			_noWorkspacesLabel = new Label(GettextCatalog.GetString("No Workspaces"))   
			{
                Margin = new WidgetSpacing(6, 0, 0, 0)
            };

            _workspaceLabel.Visible = false;
            _workspaceComboBox.Visible = false;

			_foldersView = new TreeView
			{
				BorderVisible = false,
				WidthRequest = 300
			};

			_baseItemField = new DataField<BaseItem>();
			_iconField = new DataField<Xwt.Drawing.Image>();
			_nameField = new DataField<string>();
			_foldersStore = new TreeStore(_baseItemField, _iconField, _nameField);

			_folderDetailsView = new TreeView
			{
				BorderVisible = false
			};

			_extendedItemField = new DataField<ExtendedItem>();
			_itemIconField = new DataField<Xwt.Drawing.Image>();
			_itemNameField = new DataField<string>();
			_pendingField = new DataField<string>();
			_userField = new DataField<string>();
			_latestField = new DataField<string>();
			_lastCheckInField = new DataField<string>();
			_folderDetailsStore = new TreeStore(_extendedItemField, _itemIconField, _itemNameField, _pendingField, _userField, _latestField, _lastCheckInField);
    
            _versionControlService = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();
        }
       
        void BuildGui()
        {          
			_workspaceComboBox.Views.Add(new TextCellView(_workspaceNameField));
            _workspaceComboBox.Views.Add(new TextCellView { MarkupField = _workspacePathField });

			_headerBox.PackStart(_addItemButton, false, false);   
			_headerBox.PackStart(_refreshButton, false, false);   
			_headerBox.PackStart(new HSeparator() { BackgroundColor = Xwt.Drawing.Colors.Gray, Margin = new WidgetSpacing(0, 6, 0, 6) }, false, false);   
            _headerBox.PackStart(_workspaceLabel, false, false);
			_headerBox.PackStart(_noWorkspacesLabel, false, false);       
			_headerBox.PackStart(_workspaceComboBox, false, false);
			_headerBox.PackStart(_manageButton, false, false);  
         
			_view.PackStart(_headerBox, false, false);

            HPaned mainBox = new HPaned();

			VBox foldersViewBox = new VBox();

			var iconColumn = new ListViewColumn(GettextCatalog.GetString("Folders"));
			iconColumn.Views.Add(new ImageCellView(_iconField));
			_foldersView.Columns.Add(iconColumn);
			_foldersView.Columns.Add("", _nameField);
         
			ScrollView scrollContainer = new ScrollView
			{
				Content = _foldersView
			};

			foldersViewBox.PackStart(scrollContainer, true, true);
			mainBox.Panel1.Content = foldersViewBox;

            VBox rightBox = new VBox();
            HBox headerRightBox = new HBox();

            headerRightBox.PackStart(new Label(GettextCatalog.GetString("Local Path") + ":"), false, false);
            rightBox.PackStart(headerRightBox, false, false);
            headerRightBox.PackStart(_localFolder, false, false);
                      
			var itemIconColumn = new ListViewColumn();
			itemIconColumn.Views.Add(new ImageCellView(_itemIconField));
			_folderDetailsView.Columns.Add(itemIconColumn);
			_folderDetailsView.Columns.Add("Name", _itemNameField);    
			_folderDetailsView.Columns.Add("Pending Change", _pendingField);
			_folderDetailsView.Columns.Add("User", _userField);
			_folderDetailsView.Columns.Add("Latest", _latestField);
			_folderDetailsView.Columns.Add("Last Check-in", _lastCheckInField);

			_folderDetailsView.SelectionMode = SelectionMode.Multiple;
			_folderDetailsView.DataSource = _folderDetailsStore;

			var listViewScollWindow = new ScrollView
			{
				Content = _folderDetailsView
			};

			rightBox.PackStart(listViewScollWindow, true, true);
			mainBox.Panel2.Content = rightBox;

            _view.PackStart(mainBox, true, true); 
		}
        
        void AttachEvents()
        {
            _workspaceComboBox.SelectionChanged += OnChangeActiveWorkspaces;
			_addItemButton.Clicked += OnAddNewItem;
            _manageButton.Clicked += OnManageWorkspaces;
            _refreshButton.Clicked += OnRefresh;
			_foldersView.SelectionChanged += OnFolderChanged;
			_foldersView.RowActivated += OnFolderItemClicked;
			_foldersView.ButtonPressed += OnFolderMouseClick;
			_folderDetailsView.RowActivated += OnFolderDetailClicked;
			_folderDetailsView.ButtonPressed += OnFolderDetailMouseClick;
        }
      
        void FireFilesChanged(List<ExtendedItem> items)
        {
            _versionControlService.RefreshWorkingRepositories();
            FileService.NotifyFilesChanged(items.Select(i => new FilePath(_currentWorkspace.Data.GetLocalPathForServerPath(i.ServerPath))), true);
        }

        void FireFilesRemoved(List<ExtendedItem> items)
        {
            _versionControlService.RefreshWorkingRepositories();
            FileService.NotifyFilesRemoved(items.Select(i => new FilePath(_currentWorkspace.Data.GetLocalPathForServerPath(i.ServerPath))));
        }

        void Refresh(BaseItem item, MenuType menuType)
        {
			if (item != null)
			{
				Application.Invoke(() =>
				{
					switch (menuType)
					{
						case MenuType.List:
							LoadFolderDetails(item.ServerPath.ParentPath);
							break;
						case MenuType.Tree:
							LoadFolderDetails(item.ServerPath);
							break;
					}
				});
			}
        }

        void Refresh(IEnumerable<BaseItem> items)
        {
            Refresh(items.FirstOrDefault(), MenuType.List);
        }
             
        void LoadWorkspaces()
        {
			var workspaces = _projectCollection.GetLocalWorkspaces();

			_workspaces.Clear();
			_workspaceStore.Clear();
			_workspaceComboBox.SelectionChanged -= OnChangeActiveWorkspaces;

			_workspaces.AddRange(workspaces);

			int i = 0;
			int activeWorkspaceRow = 0;

			foreach (var workspace in _workspaces)
			{
				var workspaceRow = _workspaceStore.AddRow();
				_workspaceStore.SetValue(workspaceRow, _workspacePathField, string.Format("     <span color='#cccccc'>{0}</span>", workspace.WorkingFolders.FirstOrDefault()?.ServerItem.ItemName));
				_workspaceStore.SetValue(workspaceRow, _workspaceNameField, workspace.Name);
				_workspaceStore.SetValue(workspaceRow, _workspaceObjectField, workspace);

				if (string.Equals(workspace.Name, _projectCollection.ActiveWorkspaceName, StringComparison.Ordinal))
				{
					activeWorkspaceRow = i;
				}

				i++;
			}
                     
			var customWorkspaceRow = _workspaceStore.AddRow();
			_workspaceStore.SetValue(customWorkspaceRow, _workspaceNameField, GettextCatalog.GetString("Create Workspace..."));
			_workspaceStore.SetValue(customWorkspaceRow, _workspaceObjectField, 1);

			customWorkspaceRow = _workspaceStore.AddRow();
			_workspaceStore.SetValue(customWorkspaceRow, _workspaceNameField, GettextCatalog.GetString("Manage Workspaces..."));
			_workspaceStore.SetValue(customWorkspaceRow, _workspaceObjectField, 2);

			_workspaceComboBox.SelectionChanged += OnChangeActiveWorkspaces;

			if (_workspaces.Count > 0)
			{
				if (!activeWorkspaceRow.Equals(0))
					_workspaceComboBox.SelectedIndex = activeWorkspaceRow;
				else
					_workspaceComboBox.SelectedIndex = 0;

				_noWorkspacesLabel.Visible = false;
				_workspaceLabel.Visible = true;
				_workspaceComboBox.Visible = true;
			}
			else
			{
				_noWorkspacesLabel.Visible = true;
				_workspaceLabel.Visible = false;
				_workspaceComboBox.Visible = false;
			}				
        }
       
		void LoadFolders()
        {
			_worker = Task.Factory.StartNew(delegate
			{
				if (!_workerCancel.Token.IsCancellationRequested)
				{
					if (_currentWorkspace == null)
						return;

					var items = _currentWorkspace.GetItems(new[] { new ItemSpec(RepositoryPath.RootPath, RecursionType.Full) },
					                                       VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder, false);
                                                           
					var root = ItemSetToHierarchItemConverter.Convert(items);
                    			
					Application.Invoke(() =>
					{
						_foldersStore.Clear();
						_foldersList.Clear();

            			var node = _foldersStore.AddNode();

            			var serverName = string.Equals(_projectCollection.Server.Name, _projectCollection.Server.Uri.OriginalString, StringComparison.OrdinalIgnoreCase)
            				? _projectCollection.Server.Uri.Host
            				: _projectCollection.Server.Name;

            			var rootName = string.Format("{0}\\{1}", serverName, _projectCollection.Name);

            			node.SetValue(_baseItemField, root.Item);
            			node.SetValue(_iconField, ImageHelper.GetRepositoryImage());
            			node.SetValue(_nameField, rootName);

						_foldersList.Add(node);

            			AddChilds(node, root.Children);

						_foldersView.DataSource = _foldersStore;
                        
						var firstNode = _foldersStore.GetFirstNode();
						_foldersView.ExpandRow(firstNode.CurrentPosition, false);
						_foldersView.SelectRow(firstNode.CurrentPosition);					
					});
				}
			}, _workerCancel.Token, TaskCreationOptions.LongRunning);
        }

		void AddChilds(TreeNavigator rootNode, List<HierarchyItem> children)
        {
			_treeLevel++;

			foreach (var child in children)
			{
				var childNode = _foldersStore.AddNode(rootNode.CurrentPosition);

				childNode.SetValue(_baseItemField, child.Item);

				if (_treeLevel == 1)
				{
					childNode.SetValue(_iconField, ImageHelper.GetRepositoryImage());
				}
				else
				{
					childNode.SetValue(_iconField, ImageHelper.GetItemImage(ItemType.Folder));
				}

				childNode.SetValue(_nameField, child.Name);

				_foldersList.Add(childNode);

				AddChilds(childNode, child.Children);
			}

			_treeLevel--;
        }

        void LoadFolderDetails(string serverPath)
        {
			_folderDetailsStore.Clear();

			var itemSet = _currentWorkspace.GetExtendedItems(new[] { new ItemSpec(serverPath, RecursionType.OneLevel) }, DeletedState.NonDeleted, ItemType.Any);
          
			foreach (var item in itemSet.Skip(1).OrderBy(i => i.ItemType).ThenBy(i => i.TargetServerItem))
            {
				var row = _folderDetailsStore.AddNode();
                
				row.SetValue(_extendedItemField, item);
				row.SetValue(_itemIconField, ImageHelper.GetItemImage(item.ItemType));
				row.SetValue(_itemNameField, item.ServerPath.ItemName);
                                 
                if (_currentWorkspace != null)
                {
                    if (item.ChangeType != ChangeType.None && !item.HasOtherPendingChange)
                    {   
						row.SetValue(_pendingField, item.ChangeType.ToString());
						row.SetValue(_userField, _currentWorkspace.Data.Owner);
                    }

                    if (item.HasOtherPendingChange)
                    {
                        var remoteChanges = _currentWorkspace.GetPendingSets(item.SourceServerItem, RecursionType.None);
                        List<string> changeNames = new List<string>();
                        List<string> userNames = new List<string>();

                        foreach (var remoteChange in remoteChanges)
                        {
                            var pChange = remoteChange.PendingChanges.FirstOrDefault();
                         
                            if (pChange == null)
                                continue;
                            
                            changeNames.Add(pChange.ChangeType.ToString());
                            userNames.Add(remoteChange.Owner);
                        }
                  
						row.SetValue(_pendingField, string.Join(", ", changeNames));
						row.SetValue(_userField, string.Join(", ", userNames));
                    }
                }

                if (!IsMapped(item.ServerPath))
                {
					row.SetValue(_latestField, GettextCatalog.GetString("Not mapped"));
                }
                else
                {
                    if (!item.IsInWorkspace)     
                    {
						row.SetValue(_latestField, GettextCatalog.GetString("Not downloaded"));
                    }
                    else
                    {
						row.SetValue(_latestField, item.IsLatest ? GettextCatalog.GetString("Yes") : GettextCatalog.GetString("No"));
                    }
                }

				row.SetValue(_lastCheckInField, item.CheckinDate.ToString("yyyy-MM-dd HH:mm"));
            }
        }

        void OnFolderChanged(object sender, EventArgs e)
        {
			if (_foldersView.SelectedRow != null)
			{
				var node = _foldersStore.GetNavigatorAt(_foldersView.SelectedRow);
				var item = node.GetValue(_baseItemField);

				LoadFolderDetails(item.ServerPath);
				ShowMappingPath(item.ServerPath);
			}
        }
        
        void OnFolderItemClicked(object o, TreeViewRowEventArgs args)
        {
			var rowPosition = args.Position;

			var isExpanded = _foldersView.IsRowExpanded(rowPosition);

            if (isExpanded)
				_foldersView.CollapseRow(rowPosition);
            else
				_foldersView.ExpandRow(rowPosition, false);
        }

        void OnFolderDetailClicked(object sender, TreeViewRowEventArgs args)
        {
			var rowPosition = args.Position;

			if (rowPosition == null)
                return;

			var navigator = _folderDetailsStore.GetNavigatorAt(rowPosition);
			var item = navigator.GetValue(_extendedItemField);

            if (item.ItemType == ItemType.Folder)
            {
				ExpandPath(item.ServerPath);
                return;
            }

            if (item.ItemType == ItemType.File)
            {
                if (IsMapped(item.ServerPath))
                {
                    if (item.IsInWorkspace)
                    {
                        if (Projects.Services.ProjectService.IsWorkspaceItemFile(new FilePath(item.LocalPath)))
                        {
                            IdeApp.Workspace.OpenWorkspaceItem(new FilePath(item.LocalPath), true);
                        }
                        else
                        {
                            IdeApp.Workbench.OpenDocument(new FilePath(item.LocalPath), null, true);
                        }
                    }
                    else
                    {
                        var filePath = DownloadItemToTemp(item);
                        if (Projects.Services.ProjectService.IsWorkspaceItemFile(new FilePath(filePath)))
                        {
                            var parentFolder = _currentWorkspace.GetExtendedItem(ItemSpec.FromServerPath(item.ServerPath.ParentPath), ItemType.Folder);
                        
                            if (parentFolder == null)
                                return;
                            
                            GetLatestVersion(new List<ExtendedItem> { parentFolder });
                            var futurePath = _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);
                            IdeApp.Workspace.OpenWorkspaceItem(new FilePath(futurePath), true);
                            filePath.Delete();
                        }
                        else
                        {
                            IdeApp.Workbench.OpenDocument(new FilePath(filePath), null, null);
                        }
                    }
                }
                else
                {
                    var filePath = DownloadItemToTemp(item);
                    IdeApp.Workbench.OpenDocument(new FilePath(filePath), null, true);
                }
            }
        }

        LocalPath DownloadItemToTemp(ExtendedItem extendedItem)
        {
            var item = _currentWorkspace.GetItem(ItemSpec.FromServerPath(extendedItem.ServerPath), ItemType.File, true);
       
            return _currentWorkspace.DownloadToTempWithName(item.ArtifactUri, item.ServerPath.ItemName);
        }
       
        [GLib.ConnectBefore]
		void OnFolderDetailMouseClick(object o, ButtonEventArgs args)
        {
			if (args.Button == PointerButton.Right && _folderDetailsView.SelectedRows.Any())
            {
                var menu = BuildListViewPopupMenu();

				if (menu.Items.Count > 0)
                    menu.Popup();
                
                args.Handled = true;
            }
        }

        [GLib.ConnectBefore]
		void OnFolderMouseClick(object o, ButtonEventArgs args)
        {
            if (args.Button == PointerButton.Right && _foldersView.SelectedRow != null)
			{
				var node = _foldersStore.GetNavigatorAt(_foldersView.CurrentEventRow);
                var item = node.GetValue(_baseItemField);
                
                var menu = BuildTreePopupMenu(item);
              
				if (menu.Items.Count > 0)
                    menu.Popup();
                
                args.Handled = true;
            }
        }

        void OnChangeActiveWorkspaces(object sender, EventArgs ev)
        {
			var row = _workspaceComboBox.SelectedIndex;
            
			if (row == -1)
				return;

			var selectedItem = _workspaceStore.GetValue(row, _workspaceObjectField);

			if (selectedItem is int)
			{
				var menuOption = Convert.ToInt32(selectedItem);

				switch (menuOption)
				{
					case 1:
						using (var dialog = new AddEditWorkspaceDialog(_projectCollection, null))
						{
							if (dialog.Run() == Command.Ok)
							{
								LoadWorkspaces();
							}
						}
						break;
					case 2:
						using (var dialog = new WorkspacesDialog(_projectCollection))
						{
							if (dialog.Run() == Command.Close)
							{
								LoadWorkspaces();
							}
						}
						break;
				}
			}
			else
			{
				if (selectedItem is WorkspaceData workspaceData)
				{
					_currentWorkspace = DependencyContainer.GetWorkspace(workspaceData, _projectCollection);
					_versionControlService.SetActiveWorkspace(_projectCollection, workspaceData.Name);

					if (_foldersView.SelectedRow != null)
					{
						var node = _foldersStore.GetNavigatorAt(_foldersView.CurrentEventRow);
						var currentItem = node.GetValue(_baseItemField);

						if (currentItem != null)
						{
							ShowMappingPath(currentItem.ServerPath);
							LoadFolderDetails(currentItem.ServerPath);
						}
					}
				}
				else
				{
					_versionControlService.SetActiveWorkspace(_projectCollection, string.Empty);
				}
			}
        }

		void OnAddNewItem(object sender, EventArgs ev)
		{
			var rowPosition = _foldersView.SelectedRow;
			var navigator = _foldersStore.GetNavigatorAt(rowPosition);
			var item = navigator.GetValue(_baseItemField);

            var path = _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);

			using (OpenFileDialog openFileDialog = new OpenFileDialog(GettextCatalog.GetString("Browse For File")))
			{
				openFileDialog.CurrentFolder = path;
				openFileDialog.Multiselect = true;

				if (openFileDialog.Run())
				{
					var files = new List<LocalPath>();
					foreach (var fileName in openFileDialog.FileNames)
					{
						if (!string.Equals(Path.GetDirectoryName(fileName), path))
						{
							var newPath = Path.Combine(path, Path.GetFileName(fileName));
							File.Copy(fileName, newPath);
							files.Add(newPath);
						}
						else
							files.Add(fileName);
					}

					_currentWorkspace.PendAdd(files, false, out ICollection<Failure> failures);

					if (failures.Any())
					{
						using (var failuresDialog = new FailuresDialog(failures))
						{
							failuresDialog.Run();
						}
					}
					else
					{
						var checkOutItems = item.ToEnumerable();

						using (var dialog = new CheckInDialog(checkOutItems, _currentWorkspace))
						{
							if (dialog.Run() == Command.Ok)
							{
								var selectedChanges = dialog.SelectedChanges;
								string comment = dialog.Comment;
								var selectedWorkItems = dialog.SelectedWorkItems;

								_worker = Task.Factory.StartNew(delegate
								{
									if (!_workerCancel.Token.IsCancellationRequested)
									{
										using (var progress = VersionControlService.GetProgressMonitor(GettextCatalog.GetString("Check In"), VersionControlOperationType.Pull))
										{
											progress.BeginTask(GettextCatalog.GetString("Check In"), 1);
                                            
											var result = _currentWorkspace.CheckIn(selectedChanges, comment, selectedWorkItems);

											foreach (var failure in result.Failures.Where(f => f.SeverityType == SeverityType.Error))
											{
												progress.ReportError(failure.Code, new Exception(failure.Message));
											}

											Refresh(item, MenuType.List);

											progress.EndTask();
											progress.ReportSuccess(GettextCatalog.GetString("The check in has been completed successfully"));
										}
									}
								}, _workerCancel.Token, TaskCreationOptions.LongRunning);     
							}
						}                                                            
					}	
				}               
			}
		}

		void ExpandPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return;
   
			TreePosition position = null;

			foreach (var folder in _foldersList)
			{
				var item = folder.GetValue(_baseItemField);

				if (string.Equals(item.ServerPath, path, StringComparison.OrdinalIgnoreCase))
				{
					position = folder.CurrentPosition;
				}
			}
                     
			if (position == null)
				return;

			_foldersView.ExpandRow(position, false);
			_foldersView.SelectRow(position);
		}
       
        void FindItem(string name)
        {				
			if (string.IsNullOrEmpty(name))
				return;
            
			TreePosition position = null;

            var firstNode = _foldersStore.GetFirstNode();
            var navigator = _foldersStore.GetNavigatorAt(firstNode.CurrentPosition);

            if (navigator.CurrentPosition == null)
                return;

			foreach (var folder in _foldersList)
			{
				var item = folder.GetValue(_baseItemField);

				if (string.Equals(item.ServerPath.ItemName, name, StringComparison.OrdinalIgnoreCase))
				{
					position = navigator.CurrentPosition;
				}
			}
           
            if (position == null)
                return;
            
			_folderDetailsView.SelectRow(position);
			_folderDetailsView.ScrollToRow(position);
        }

        bool IsMapped(RepositoryPath serverPath)
        {
            if (_currentWorkspace == null)
                return false;

            return _currentWorkspace.Data.IsServerPathMapped(serverPath);
        }

        void ShowMappingPath(RepositoryPath serverPath)
        {
            if (!IsMapped(serverPath))
            {
                _localFolder.Text = GettextCatalog.GetString("Not Mapped");
                return;
            }
          
            _localFolder.Text = _currentWorkspace.Data.GetLocalPathForServerPath(serverPath);
        }

		void OnManageWorkspaces(object sender, EventArgs e)
        {
            using (var dialog = new WorkspacesDialog(_projectCollection))
            {
                if (dialog.Run() == Command.Close)
                {
					LoadWorkspaces();
                }
            }
        }

        void OnRefresh(object sender, EventArgs e)
        {
            RepositoryPath selectedPath = null;
			TreeNavigator navigator = null;

			if (_foldersView.SelectedRow != null)
			{
				navigator = _foldersStore.GetNavigatorAt(_foldersView.SelectedRow);            
				selectedPath = navigator.GetValue(_baseItemField).ServerPath;
			}

			LoadFolders();

            if (selectedPath != null)
				ExpandPath(selectedPath);
        }

        #region Popup Menu

        enum MenuType
        {
            Tree,
            List
        }

        Menu BuildListViewPopupMenu()
        {
            Menu menu = new Menu();

            var items = new List<ExtendedItem>();
            
			foreach (var path in _folderDetailsView.SelectedRows)
            {            
				var node = _folderDetailsStore.GetNavigatorAt(path);
				var extendedItem = node.GetValue(_extendedItemField);
				items.Add(extendedItem);
            }

            if (items.All(i => IsMapped(i.ServerPath)))
            {
                foreach (var item in GetGroup(items))
                {
					menu.Items.Add(item);
                }

				menu.Items.Add(new SeparatorMenuItem());

                foreach (var item in EditGroup(items))
                {
					menu.Items.Add(item);
                }

                if (items.Count == 1)
                {
                    foreach (var menuItem in ForlderMenuItems(items[0], MenuType.List))
                    {
						menu.Items.Add(menuItem);
                    }
                }
            }
            else
            {
				menu.Items.Add(NotMappedMenu(items, MenuType.List));
            }

            return menu;
        }

        IEnumerable<MenuItem> GetGroup(List<ExtendedItem> items)
        {
            MenuItem getLatestVersionItem = new MenuItem(GettextCatalog.GetString("Get Latest Version"));
            getLatestVersionItem.Clicked += (sender, e) => GetLatestVersion(items);

            yield return getLatestVersionItem;

            MenuItem forceGetLatestVersionItem = new MenuItem(GettextCatalog.GetString("Get Specific Version"));
			forceGetLatestVersionItem.Clicked += (sender, e) => ForceGetLatestVersion(items);

            yield return forceGetLatestVersionItem;
        }

        void GetLatestVersion(List<ExtendedItem> items)
        {
            List<GetRequest> requests = new List<GetRequest>();

            foreach (var item in items)
            {
                RecursionType recursion = item.ItemType == ItemType.File ? RecursionType.None : RecursionType.Full;
                requests.Add(new GetRequest(item.ServerPath, recursion, VersionSpec.Latest));
            }

			_worker = Task.Factory.StartNew(delegate
            {
				if (!_workerCancel.Token.IsCancellationRequested)
				{
					using (var progress = VersionControlService.GetProgressMonitor(GettextCatalog.GetString("Get"), VersionControlOperationType.Pull))
					{
						var option = GetOptions.None;
						progress.Log.WriteLine("Downloading items. GetOption: " + option);

						foreach (var request in requests)
						{
							progress.Log.WriteLine(request);
						}

						_currentWorkspace.Get(requests, option);
						Refresh(items);

						progress.ReportSuccess(GettextCatalog.GetString("The download has been completed successfully"));
					}
				}
            }, _workerCancel.Token, TaskCreationOptions.LongRunning);           
        }

        void ForceGetLatestVersion(List<ExtendedItem> items)
        {
            using (var specVersionDialog = new GetSpecVersionDialog(_currentWorkspace))
            {
                specVersionDialog.AddData(items);

                if (specVersionDialog.Run() == Command.Ok)
                {
                    Refresh(items);
                }
            }
        }

        IEnumerable<MenuItem> EditGroup(List<ExtendedItem> items)
        {
            //Check Out
            var checkOutItems = items.Where(i => i.ChangeType == ChangeType.None || i.ChangeType == ChangeType.Lock || i.ItemType == ItemType.Folder).ToList();
        
            if (checkOutItems.Any())
            {  
				MenuItem checkOutItem = new MenuItem(GettextCatalog.GetString("Check out items"));
              
				checkOutItem.Clicked += (sender, e) =>
                {
					using (var dialog = new CheckOutDialog(checkOutItems, _currentWorkspace))
					{
						if (dialog.Run() == Command.Ok)
						{
							var itemsToCheckOut = dialog.SelectedItems;
							CheckOut(itemsToCheckOut, dialog.LockLevel);
						}
					}                   
                };

                yield return checkOutItem;
            }

            // Check In
            var checkInItems = items.Where(i => !i.ChangeType.HasFlag(ChangeType.None)).ToList();

            if (checkInItems.Any())
            {
                MenuItem checkinItem = new MenuItem(GettextCatalog.GetString("Check In"));

				checkinItem.Clicked += (sender, e) =>
                {
                    using (var dialog = new CheckInDialog(checkOutItems, _currentWorkspace))
                    {
                        if (dialog.Run() == Command.Ok)
                        {
							var selectedChanges = dialog.SelectedChanges;
                            string comment = dialog.Comment;
                            var selectedWorkItems = dialog.SelectedWorkItems;

							_worker = Task.Factory.StartNew(delegate
							{
								if (!_workerCancel.Token.IsCancellationRequested)
								{
									using (var progress = VersionControlService.GetProgressMonitor(GettextCatalog.GetString("Check In"), VersionControlOperationType.Pull))
									{
										progress.BeginTask(GettextCatalog.GetString("Check In"), 1);
                                        
										var result = _currentWorkspace.CheckIn(selectedChanges, comment, selectedWorkItems);

										foreach (var failure in result.Failures.Where(f => f.SeverityType == SeverityType.Error))
										{
											progress.ReportError(failure.Code, new Exception(failure.Message));
										}
										                       
										FireFilesChanged(checkInItems);
										Refresh(items);

										progress.EndTask();
										progress.ReportSuccess(GettextCatalog.GetString("The checkin has been completed successfully"));
									}
								}
							}, _workerCancel.Token, TaskCreationOptions.LongRunning);
                        }
                    }
                };

                yield return checkinItem;
            }

            //Lock
            var lockItems = items.Where(i => !i.IsLocked).ToList();
         
            if (lockItems.Any())
            {
                MenuItem lockItem = new MenuItem(GettextCatalog.GetString("Lock"));
               
				lockItem.Clicked += (sender, e) =>
                {
                    var itemsToLock = items;
                    var lockLevel = LockLevel.CheckOut;

                    if (itemsToLock.Count > 1)
                    {
                        using (var dialog = new LockDialog(itemsToLock))
                        {
                            if (dialog.Run() == Command.Ok)
                            {
                                Lock(dialog.SelectedItems, dialog.LockLevel);
                            }
                        }
                    }
                    else
                    {
                        Lock(itemsToLock, lockLevel);
                    }
                };

                yield return lockItem;
            }

            //UnLock
            var unLockItems = items.Where(i => i.IsLocked && !i.HasOtherPendingChange).ToList();

            if (unLockItems.Any())
            {
                MenuItem unLockItem = new MenuItem(GettextCatalog.GetString("UnLock"));
              
				unLockItem.Clicked += (sender, e) =>
                {
					_worker = Task.Factory.StartNew(delegate
                    {
                        if (!_workerCancel.Token.IsCancellationRequested)
                        {
							using (var progress = VersionControlService.GetProgressMonitor(GettextCatalog.GetString("Unlock"), VersionControlOperationType.Pull))
                            {
								progress.BeginTask(GettextCatalog.GetString("Unlocking files..."), unLockItems.Count);

								_currentWorkspace.UnLockItems(unLockItems.Select(i => i.ServerPath));
                                FireFilesChanged(unLockItems);
                                Refresh(items);

                                progress.EndTask();
								progress.ReportSuccess(GettextCatalog.GetString("The files have been unlocked successfully"));
                            }
                        }
                    }, _workerCancel.Token, TaskCreationOptions.LongRunning); 
                };

                yield return unLockItem;
            }

            //Rename
            var ableToRename = items.FirstOrDefault(i => !i.ChangeType.HasFlag(ChangeType.Delete));
          
            if (ableToRename != null)
            {
                MenuItem renameItem = new MenuItem(GettextCatalog.GetString("Rename"));
               
				renameItem.Clicked += (sender, e) =>
                {
                    using (var dialog = new RenameDialog(ableToRename))
                    {
                        if (dialog.Run() == Command.Ok)
                        {                         
							_currentWorkspace.PendRename(ableToRename.LocalPath, dialog.NewPath, out ICollection<Failure> failures);

							if (failures != null && failures.Any(f => f.SeverityType == SeverityType.Error))
                            {
                                foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
                                {
                                    MessageService.ShowError(failure.Message);
                                }
                            }

                            FireFilesChanged(new List<ExtendedItem> { ableToRename });
                            Refresh(items);
                        }
                    }
                };

                yield return renameItem;
            }

            //Delete
            var ableToDelete = items.Where(i => !i.ChangeType.HasFlag(ChangeType.Delete)).ToList();
           
            if (ableToDelete.Any())
            {
                MenuItem deleteItem = new MenuItem(GettextCatalog.GetString("Delete"));
               
				deleteItem.Clicked += (sender, e) =>
                {
                    if (MessageService.Confirm(GettextCatalog.GetString("Are you sure you want to delete selected files?"), AlertButton.Yes))
                    {
						_currentWorkspace.PendDelete(items.Select(x => x.LocalPath), RecursionType.Full, false, out ICollection<Failure> failures);

						var errors = failures.Any(f => f.SeverityType == SeverityType.Error);

                        if (errors)
                        {
                            var failuresDialog = new FailuresDialog(failures);
                            failuresDialog.Show();
                        }

                        FireFilesRemoved(items);
                        Refresh(items);
                    }
                };
              
                yield return deleteItem;
            }

            //Undo
            var undoItems = items.Where(i => !i.ChangeType.HasFlag(ChangeType.None) || i.ItemType == ItemType.Folder).ToList();

            if (undoItems.Any())
            {
                MenuItem undoItem = new MenuItem(GettextCatalog.GetString("Undo Changes"));
             
				undoItem.Clicked += (sender, e) =>
                {
                    using (var dialog = new UndoDialog(undoItems, _currentWorkspace))
                    {
                        if (dialog.Run() == Command.Ok)
                        {
                            var changesToUndo = dialog.SelectedItems;
                            var itemSpecs = new List<ItemSpec>();

                            foreach (var change in changesToUndo)
                            {
                                itemSpecs.Add(new ItemSpec(change.LocalItem, change.ItemType == ItemType.File ? RecursionType.None : RecursionType.Full));
                            }

							_worker = Task.Factory.StartNew(delegate
							{
								if (!_workerCancel.Token.IsCancellationRequested)
								{
									using (var progress = VersionControlService.GetProgressMonitor(GettextCatalog.GetString("Undo"), VersionControlOperationType.Pull))
									{                    
										progress.BeginTask(GettextCatalog.GetString("Undoing..."), unLockItems.Count);
										_currentWorkspace.Undo(itemSpecs);
                                        									
										FireFilesChanged(undoItems);
                                        Refresh(items);

										progress.EndTask();
										progress.ReportSuccess(GettextCatalog.GetString("The changes have been undone successfully"));
									}
								}
							}, _workerCancel.Token, TaskCreationOptions.LongRunning);
                        }
                    }
                };

                yield return undoItem;
            }
        }

        void Lock(List<ExtendedItem> itemsTolock, LockLevel lockLevel)
        {
			_worker = Task.Factory.StartNew(delegate
			{
				if (!_workerCancel.Token.IsCancellationRequested)
				{
					using (var progress = VersionControlService.GetProgressMonitor(GettextCatalog.GetString("Lock"), VersionControlOperationType.Pull))
					{
						progress.BeginTask(GettextCatalog.GetString("Locking files..."), itemsTolock.Count);

						_currentWorkspace.LockItems(itemsTolock.Select(i => i.ServerPath), lockLevel);

						FireFilesChanged(itemsTolock);
						Refresh(itemsTolock);

						progress.EndTask();
						progress.ReportSuccess(GettextCatalog.GetString("The files have been locked successfully"));
					}
				}
			}, _workerCancel.Token, TaskCreationOptions.LongRunning);
        }

        void CheckOut(List<ExtendedItem> itemsToCheckOut, LockLevel lockLevel)
        {
			_worker = Task.Factory.StartNew(delegate
			{
				if (!_workerCancel.Token.IsCancellationRequested)
				{
					using (var progress = VersionControlService.GetProgressMonitor(GettextCatalog.GetString("Check Out"), VersionControlOperationType.Pull))
					{
						progress.BeginTask(GettextCatalog.GetString("Check Out"), itemsToCheckOut.Count);

						foreach (var item in itemsToCheckOut)
						{
							var path = item.IsInWorkspace ? item.LocalPath : _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);
							_currentWorkspace.Get(new GetRequest(item.ServerPath, RecursionType.Full, VersionSpec.Latest), GetOptions.None);
							progress.Log.WriteLine(GettextCatalog.GetString("Check out item: ") + item.ServerPath);

							_currentWorkspace.PendEdit(path.ToEnumerable(), RecursionType.Full, lockLevel, out ICollection<Failure> failures);

							if (failures != null && failures.Any())
							{
								if (failures.Any(f => f.SeverityType == SeverityType.Error))
								{
									foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
									{
										progress.ReportError(failure.Code, new Exception(failure.Message));
									}

									break;
								}

								foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Warning))
								{
									progress.ReportWarning(failure.Message);
								}
							}
						}

						FireFilesChanged(itemsToCheckOut);
						Refresh(itemsToCheckOut);

						progress.EndTask();
						progress.ReportSuccess(GettextCatalog.GetString("The checkout have been completed successfully"));
					}
				}
			}, _workerCancel.Token, TaskCreationOptions.LongRunning);
        }

        IEnumerable<MenuItem> ForlderMenuItems(BaseItem item, MenuType menuType)
        {
            if (item.ItemType != ItemType.Folder)
                yield break;
            
            yield return CreateAddFileMenuItem(item, menuType);
            yield return new SeparatorMenuItem();
            yield return CreateOpenFolderMenuItem(item);
        }


        MenuItem CreateAddFileMenuItem(BaseItem item, MenuType menuType)
        {
            MenuItem addItem = new MenuItem(GettextCatalog.GetString("Add New Item"));
           
			addItem.Clicked += (sender, e) =>
            {
                var path = _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);

				using (OpenFileDialog openFileDialog = new OpenFileDialog(GettextCatalog.GetString("Browse For File")))
                {
                    openFileDialog.CurrentFolder = path;
                    openFileDialog.Multiselect = true;
                 
                    if (openFileDialog.Run())
                    {
                        var files = new List<LocalPath>();
                        foreach (var fileName in openFileDialog.FileNames)
                        {
                            if (!string.Equals(Path.GetDirectoryName(fileName), path))
                            {
                                var newPath = Path.Combine(path, Path.GetFileName(fileName));
                                File.Copy(fileName, newPath);
                                files.Add(newPath);
                            }
                            else
                                files.Add(fileName);
                        }

						_currentWorkspace.PendAdd(files, false, out ICollection<Failure> failures);

						if (failures.Any())
                        {
                            var failuresDialog = new FailuresDialog(failures);
                            failuresDialog.Show();
                        }
                        else
                        {
                            var checkOutItems = item.ToEnumerable();

                            using (var dialog = new CheckInDialog(checkOutItems, _currentWorkspace))
                            {
                                if (dialog.Run() == Command.Ok)
								{ 
									var selectedChanges = dialog.SelectedChanges;
                                    string comment = dialog.Comment;
                                    var selectedWorkItems = dialog.SelectedWorkItems;

									_worker = Task.Factory.StartNew(delegate
									{
										if (!_workerCancel.Token.IsCancellationRequested)
										{
											using (var progress = VersionControlService.GetProgressMonitor(GettextCatalog.GetString("Check In"), VersionControlOperationType.Pull))
											{
												progress.BeginTask(GettextCatalog.GetString("Check In"), 1);

												var result = _currentWorkspace.CheckIn(selectedChanges, comment, selectedWorkItems);

												foreach (var failure in result.Failures.Where(f => f.SeverityType == SeverityType.Error))
												{
													progress.ReportError(failure.Code, new Exception(failure.Message));
												}

												Refresh(item, menuType);

												progress.EndTask();
												progress.ReportSuccess(GettextCatalog.GetString("The checkin have been completed successfully"));
											}
										}
									}, _workerCancel.Token, TaskCreationOptions.LongRunning);
                                }
                            }                                           
                        }
                    }
                }
            };

            return addItem;
        }

        MenuItem CreateOpenFolderMenuItem(BaseItem item)
        {
            MenuItem openFolder = new MenuItem(GettextCatalog.GetString("Open Folder"));
          
			openFolder.Clicked += (sender, e) =>
            {
                var path = _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);
                DesktopService.OpenFolder(new FilePath(path));
            };

            return openFolder;
        }

        MenuItem NotMappedMenu(IEnumerable<BaseItem> items, MenuType menuType)
        {
            MenuItem mapItem = new MenuItem(GettextCatalog.GetString("Map"));
            var item = items.FirstOrDefault(i => i.ItemType == ItemType.Folder);
			mapItem.Clicked += (sender, e) => MapItem(item, menuType);
           
            return mapItem;
        }

        void MapItem(BaseItem item, MenuType menuType)
        {
            if (_currentWorkspace == null || item == null)
                return;
            
			using (Xwt.SelectFolderDialog folderSelect = new Xwt.SelectFolderDialog(GettextCatalog.GetString("Browse For Folder")))
            {
                folderSelect.Multiselect = false;
                folderSelect.CanCreateFolders = true;
             
                if (folderSelect.Run())
                {
                    _currentWorkspace.Map(item.ServerPath, folderSelect.Folder);
                }

                Refresh(item, menuType);
            }
        }
       
        Menu BuildTreePopupMenu(BaseItem item)
        {
            Menu menu = new Menu();
        
            if (item == null || item.ServerPath == RepositoryPath.RootPath)
                return menu;
            
            if (!IsMapped(item.ServerPath))
            {
                menu.Items.Add(NotMappedMenu(item.ToEnumerable(), MenuType.Tree));
            }
            else
            {
                foreach (var menuItem in ForlderMenuItems(item, MenuType.Tree))
                {
					menu.Items.Add(menuItem);
                }
            }
     
            return menu;
        }

        #endregion

        #endregion
    }
}