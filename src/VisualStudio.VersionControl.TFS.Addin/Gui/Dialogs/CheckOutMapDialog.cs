// CheckOutMapDialog.cs
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
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
	/// <summary>
    /// Check out map dialog.
    /// </summary>
	public class CheckOutMapDialog : Dialog
    {
        Label _titleLabel;   
        ComboBox _accountComboBox;
		DataField<Image> _accountIconField;
		DataField<string> _accountNameField;
        DataField<object> _accountObjectField;
        ListStore _accountStore;
        ComboBox _projectCollectionComboBox;
        ListView _projectsListView;
        DataField<Image> _projectType;
        DataField<string> _projectName;
        DataField<ProjectInfo> _projectItem;
        ListStore _projectsStore; 
        TreeView _filesView;
        Spinner _projectsSpinner;
        DataField<bool> _isCheckedField;
        DataField<string> _fileName;
        DataField<BaseItem> _baseItem;
        TreeStore _filesStore;
        Button _refreshButton;
        ComboBox _workspaceComboBox;
		DataField<string> _workspaceNameField;
        DataField<string> _workspacePathField;
        DataField<object> _workspaceObjectField;
        ListStore _workspaceStore;
        TextEntry _localPathEntry;
        Button _browseButton;
        Button _checkoutButton;
        
        TeamFoundationServer _server;
		List<TeamFoundationServer> _accounts;
        ProjectCollection _projectCollection;
		List<BaseItem> _selectedItems;
		List<WorkspaceData> _workspaces;
        Task _worker;
        CancellationTokenSource _workerCancel;

        IWorkspaceService _currentWorkspace;
        TeamFoundationServerVersionControlService _versionControlService;
       
        internal CheckOutMapDialog(TeamFoundationServerVersionControlService versionControlService)
        {        
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor(true))
            {
				monitor.BeginTask(GettextCatalog.GetString("Loading..."), 2);
				Init(versionControlService);            
                BuildGui();            
                AttachEvents();            
				LoadAccounts(); 
				monitor.Step(1);
                LoadProjectCollection();            
                LoadWorkspaces();            
				LoadProjects(); 
				monitor.EndTask();
            }
        }

        /// <summary>
        /// Ons the Dialog closed.
        /// </summary>
		protected override void OnClosed()
		{
            _workerCancel?.Cancel();

			base.OnClosed();
		}

        /// <summary>
		/// Init CheckOutMapDialog.
        /// </summary>
        /// <param name="versionControlService">Version control service.</param>
		void Init(TeamFoundationServerVersionControlService versionControlService)
        {
            _versionControlService = versionControlService;

            _workerCancel = new CancellationTokenSource();
			_selectedItems = new List<BaseItem>();
			_workspaces = new List<WorkspaceData>();
			_accounts = new List<TeamFoundationServer>();

            _titleLabel = new Label(GettextCatalog.GetString("Your Hosted Repositories"))
            {
				MinHeight = 24,
                Margin = new WidgetSpacing(0, 0, 0, 12)
            };

            _accountComboBox = new ComboBox
            {
                MinWidth = 620
            };

			_accountIconField = new DataField<Image>();
			_accountNameField = new DataField<string>();
			_accountObjectField = new DataField<object>();
			_accountStore = new ListStore(_accountIconField, _accountNameField, _accountObjectField);

			_accountComboBox.ItemsSource = _accountStore;

            _projectCollectionComboBox = new ComboBox
            {
                MinWidth = 620
            };

            _projectsListView = new ListView();
            _projectType = new DataField<Image>();
            _projectName = new DataField<string>();
            _projectItem = new DataField<ProjectInfo>();
            _projectsStore = new ListStore(_projectType, _projectName, _projectItem);

			_projectsSpinner = new Spinner
			{
				HeightRequest = 24,
				WidthRequest = 24,
				Animate = true,
				HorizontalPlacement = WidgetPlacement.Center,
				VerticalPlacement = WidgetPlacement.Center
			};
           
            _filesView = new TreeView
            {
                ExpandHorizontal = true,
                ExpandVertical = true
            };

            _isCheckedField = new DataField<bool>();
            _fileName = new DataField<string>();
            _baseItem = new DataField<BaseItem>();
            _filesStore = new TreeStore(_isCheckedField, _fileName, _baseItem);

			_refreshButton = new Button(GettextCatalog.GetString("Refresh"))
			{
				WidthRequest = 100
			};

			_workspaceComboBox = new ComboBox
            {
                MinWidth = 150
            };

			_workspaceNameField = new DataField<string>();
            _workspacePathField = new DataField<string>();
            _workspaceObjectField = new DataField<object>();

            _workspaceStore = new ListStore(_workspaceNameField, _workspacePathField, _workspaceObjectField);
            _workspaceComboBox.ItemsSource = _workspaceStore;

            _localPathEntry = new TextEntry
            { 
				VerticalPlacement = WidgetPlacement.Center,
                ReadOnly = true,
                MinWidth = 400
            };

			_localPathEntry.Font = _localPathEntry.Font.WithScaledSize(1.2);
			_localPathEntry.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

			_browseButton = new Button(GettextCatalog.GetString("Browse..."))
            {
                HorizontalPlacement = WidgetPlacement.End
            };
        } 

        /// <summary>
		/// Builds the CheckOutMapDialog GUI.
        /// </summary>
        void BuildGui()
        {
            Title = GettextCatalog.GetString("Checkout from VSTS or TFS Repository");
           
            VBox content = new VBox
            {
				Margin = new WidgetSpacing(12, 12, 12, 0),
				HeightRequest = 560,
                MinWidth = 700
            };

            content.PackStart(_titleLabel);

            VBox selectorBox = new VBox();

            HBox accountBox = new HBox();
          
			Label accountLabel = new Label(GettextCatalog.GetString("Account:"))
            {
                WidthRequest = 60,
                HorizontalPlacement = WidgetPlacement.End,
                Margin = new WidgetSpacing(230, 0, 0, 0)
            };

            accountBox.PackStart(accountLabel);
            
			_accountComboBox.Views.Add(new ImageCellView(_accountIconField));
			_accountComboBox.Views.Add(new TextCellView(_accountNameField));

            accountBox.PackEnd(_accountComboBox);
            selectorBox.PackStart(accountBox);

            HBox collectionBox = new HBox();
          
			Label collectionLabel = new Label(GettextCatalog.GetString("Collection:"))
            {
                WidthRequest = 60,
                HorizontalPlacement = WidgetPlacement.End,
                Margin = new WidgetSpacing(230, 0, 0, 0)
            };

            collectionBox.PackStart(collectionLabel);
            collectionBox.PackEnd(_projectCollectionComboBox);

            selectorBox.PackEnd(collectionBox);
            content.PackStart(selectorBox);

            HBox mainBox = new HBox();

            FrameBox listViewFrame = new FrameBox
            {
                BorderColor = Colors.LightGray,
                BorderWidthTop = 1,   
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                WidthRequest = 300
            };

            VBox listViewBox = new VBox
            {
                BackgroundColor = Colors.White
            };

            FrameBox listViewHeaderFrame = new FrameBox
            {
                BorderColor = Colors.LightGray,
                BorderWidthBottom = 1,
                WidthRequest = 300
            };

            listViewHeaderFrame.Content = new Label(GettextCatalog.GetString("Team Project")) { HeightRequest = 24, Margin = new WidgetSpacing(6) };
            listViewBox.PackStart(listViewHeaderFrame);
			listViewBox.PackStart(_projectsSpinner);
            _projectsListView.HeadersVisible = false;
            _projectsListView.BorderVisible = false;
            _projectsListView.MinHeight = 300;
            _projectsListView.VerticalPlacement = WidgetPlacement.Fill;
            _projectsListView.Margin = new WidgetSpacing(0);
            var projectTypeColumn = new ListViewColumn("Type");
            projectTypeColumn.Views.Add(new ImageCellView(_projectType));
            _projectsListView.Columns.Add(projectTypeColumn);
            _projectsListView.Columns.Add("Name", _projectName);
            _projectsListView.DataSource = _projectsStore;
            listViewBox.PackStart(_projectsListView);
            listViewFrame.Content = listViewBox;
            mainBox.PackStart(listViewFrame);

            FrameBox treeViewFrame = new FrameBox
            {
                BorderColor = Colors.LightGray,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                Margin = new WidgetSpacing(-6, 0, 0, 0)
            };

            VBox treeViewBox = new VBox
            {
                BackgroundColor = Colors.White,
                Margin = 0
            };

            _filesView.HeadersVisible = false;
            _filesView.MinHeight = 300;
            _filesView.WidthRequest = 620;
            _filesView.BorderVisible = false;
            _filesView.UseAlternatingRowColors = true;
            _filesView.GridLinesVisible = GridLines.None;
            _filesView.VerticalPlacement = WidgetPlacement.Fill;
            _filesView.Margin = new WidgetSpacing(0);

            FrameBox treeViewHeaderFrame = new FrameBox
            {
                BorderColor = Colors.LightGray,
                BorderWidthBottom = 1
            };

            treeViewHeaderFrame.Content = new Label(GettextCatalog.GetString("Directory for Mapping")) { HeightRequest = 24, Margin = new WidgetSpacing(6) };
            treeViewBox.PackStart(treeViewHeaderFrame);
                      
            var checkView = new CheckBoxCellView(_isCheckedField)
            {
                Editable = true
            };

            // CheckBox status changed
			checkView.Toggled += (sender, e) =>
			{            
				var astore = (TreeStore)_filesView.DataSource;
				var node = astore.GetNavigatorAt(_filesView.CurrentEventRow);

				var baseItem = node.GetValue(_baseItem);
				var isChecked = node.GetValue(_isCheckedField);

				if (!isChecked)
                {
					if (!_selectedItems.Contains(baseItem))
					{
						_selectedItems.Add(baseItem);
					}
                }
                else
                {
					_selectedItems.Remove(baseItem);
                }            
            };

            _filesView.Columns.Add("", checkView);
            _filesView.Columns.Add("Name", _fileName);
            _filesView.DataSource =_filesStore;
            treeViewBox.PackStart(_filesView);

            treeViewFrame.Content = treeViewBox;
            mainBox.PackStart(treeViewFrame);

            content.PackStart(mainBox);

            FrameBox refreshBox = new FrameBox
            {
                BorderColor = Colors.LightGray,
                BorderWidth = 1,
                Margin = new WidgetSpacing(0, -6, 0, 0)
            };

            _refreshButton.BackgroundColor = Colors.White;
            _refreshButton.HorizontalPlacement = WidgetPlacement.End;
            _refreshButton.Style = ButtonStyle.Flat;
            _refreshButton.WidthRequest = 60;
            refreshBox.Content = _refreshButton;
            content.PackStart(refreshBox);

            HBox workspaceBox = new HBox();
            workspaceBox.PackStart(new Label(GettextCatalog.GetString("Workspace:")));

			_workspaceComboBox.Views.Add(new TextCellView(_workspaceNameField));
            _workspaceComboBox.Views.Add(new TextCellView { MarkupField = _workspacePathField });

            workspaceBox.PackStart(_workspaceComboBox);
            workspaceBox.PackStart(new Label(GettextCatalog.GetString("Local Path:")) { Margin = new WidgetSpacing(12, 0, 0, 0 ) });
            workspaceBox.PackStart(_localPathEntry);
            workspaceBox.PackEnd(_browseButton);
            content.PackStart(workspaceBox);

            HBox buttonBox = new HBox
            {
                VerticalPlacement = WidgetPlacement.End
            };

            Button cancelButton = new Button(GettextCatalog.GetString("Cancel"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            cancelButton.HorizontalPlacement = WidgetPlacement.Start;
            cancelButton.Clicked += (sender, e) => Respond(Command.Cancel);

            _checkoutButton = new Button(GettextCatalog.GetString("Checkout"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            _checkoutButton.HorizontalPlacement = WidgetPlacement.End;

            buttonBox.PackStart(cancelButton);
            buttonBox.PackEnd(_checkoutButton);

            content.PackStart(buttonBox);

            Content = content;
            Resizable = false;
        }

        void AttachEvents()
        {
            _accountComboBox.SelectionChanged += OnChangeAccount;
            _projectCollectionComboBox.SelectionChanged += OnChangeProjectCollection;
            _workspaceComboBox.SelectionChanged += OnChangeWorkspace;
            _projectsListView.SelectionChanged += OnChangeProject;
            _browseButton.Clicked += OnBrowse;
            _refreshButton.Clicked += OnRefresh;
            _checkoutButton.Clicked += OnCheckout;  // Checkout, map & Get code
        }
       
        void OnChangeAccount(object sender, EventArgs args)
        {
			var row = _accountComboBox.SelectedIndex;

            if (row == -1)
                return;

			var selectedItem = _accountStore.GetValue(row, _accountObjectField);
            
			if (selectedItem is int)
            {
                using (var dialog = new ConnectToServerDialog())
                {
                    dialog.Run(MessageDialog.RootWindow);
                }
            }
            else
            {
				_server = (TeamFoundationServer)selectedItem;

				ClearProjects();
				LoadProjectCollection();
                LoadWorkspaces();
                LoadProjects();
            }         
        }

        void OnChangeProjectCollection(object sender, EventArgs args)
        {
            if (_projectCollectionComboBox.SelectedItem != null)
            {
                _projectCollection = (ProjectCollection)_projectCollectionComboBox.SelectedItem;
            }
        }

        void OnChangeWorkspace(object sender, EventArgs args)
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
							if (dialog.Run(this) == Command.Ok)
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
					default:
						break;
				}
			}
			else
            {
				if (selectedItem is WorkspaceData workspaceData)
				{
					_currentWorkspace = DependencyContainer.GetWorkspace(workspaceData, _projectCollection);

					if (_currentWorkspace.Data.WorkingFolders.Any())
					{
						var localItem = _currentWorkspace.Data.WorkingFolders.FirstOrDefault().LocalItem;
						var localPath = localItem.ToString();
						_localPathEntry.Text = localPath;
					}
				}
                else
                {
                    _localPathEntry.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
			}
		}

		void OnChangeProject(object sender, EventArgs args)
        {
            var row = _projectsListView.SelectedRow;

            if (row != -1)
            {
                var projectItem = _projectsStore.GetValue(row, _projectItem);

                if (projectItem != null)
                {
                    LoadFolders(string.Format("$/{0}", projectItem.Name));
                }
            }
        }

        void OnBrowse(object sender, EventArgs args)
        {
            using (SelectFolderDialog folderSelect = new SelectFolderDialog("Browse For Folder"))
            {
                if (folderSelect.Run(this))
                {
                    _localPathEntry.Text = folderSelect.Folder;
                }
            }
        }

        void OnRefresh(object sender, EventArgs args)
        {
			Refresh();
        }

        /// <summary>
        /// Refresh accounts, project collections, workspaces and folders.
        /// </summary>
        void Refresh()
		{
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor(true))
            {
                monitor.BeginTask("Loading...", 2);
                LoadAccounts();
                LoadProjectCollection();
                monitor.Step(1);
                LoadWorkspaces();
                LoadProjects();
                monitor.EndTask();
            }
		}

        /// <summary>
        /// Ons the checkout.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="args">Arguments.</param>
        void OnCheckout(object sender, EventArgs args)
        {
			if (_currentWorkspace == null)               
			{
				MessageService.ShowWarning("It is mandatory to have an existing workspace.");
				return;               
			}

			if (!_selectedItems.Any())
            {
				MessageService.ShowWarning("No selected path found.");
                return;
            }

			var localPath = _localPathEntry.Text;

			if (string.IsNullOrEmpty(localPath))
            {
				MessageService.ShowWarning("The local path is not valid.");
                return;
            }

			Respond(Command.Ok);

			_worker = Task.Factory.StartNew(delegate
			{
				if (!_workerCancel.Token.IsCancellationRequested)
				{
					using (var progress = VersionControlService.GetProgressMonitor("Check Out", VersionControlOperationType.Pull))
					{
						progress.BeginTask("Check Out", _selectedItems.Count);

						try
						{
							// Map
							foreach (var item in _selectedItems)
							{
								_currentWorkspace.Map(item.ServerPath, localPath);
							}

							// Checkout
							foreach (var item in _selectedItems)
							{
								var path = _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);
								_currentWorkspace.Get(new GetRequest(item.ServerPath, RecursionType.Full, VersionSpec.Latest), GetOptions.None);
								progress.Log.WriteLine("Check out item: " + item.ServerPath);

								_currentWorkspace.PendEdit(path.ToEnumerable(), RecursionType.Full, LockLevel.CheckOut, out ICollection<Failure> failures);

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

							progress.EndTask();
                            progress.ReportSuccess("Finish Check Out.");
						}
						catch(Exception ex)
						{
							progress.ReportError(ex.Message);
						}                        
					}
				}
			}, _workerCancel.Token, TaskCreationOptions.LongRunning);           
        }

        /// <summary>
        /// Loads the server accounts.
        /// </summary>
        void LoadAccounts()
        {
			var accounts = _versionControlService.Servers;

			_accounts.Clear();
            _accountStore.Clear();
			_accountComboBox.SelectionChanged -= OnChangeAccount;

			_accounts.AddRange(accounts);

			if (_accounts.Any())
            {
				foreach (var server in _accounts)
                {
					var accountRow = _accountStore.AddRow();

					var icon = GetAccountIcon(server.UserName);

					if(icon != null)
					{
						_accountStore.SetValue(accountRow, _accountIconField, icon.WithSize(16, 16));
						_accountStore.SetValue(accountRow, _accountNameField, string.Format(" {0}", server.UserName));
					}
					else
					{
						_accountStore.SetValue(accountRow, _accountNameField, server.UserName);
					}

					_accountStore.SetValue(accountRow, _accountObjectField, server);
                }

				var customAccountRow = _accountStore.AddRow();
				_accountStore.SetValue(customAccountRow, _accountNameField, "Manage Workspaces...");
				_accountStore.SetValue(customAccountRow, _accountObjectField, 2);

				_accountComboBox.SelectionChanged += OnChangeAccount;

				_accountComboBox.SelectedIndex = 0;
            }
        }
        
        /// <summary>
		/// Gets the account icon (Microsoft).
        /// </summary>
        /// <returns>The account icon.</returns>
        /// <param name="username">Username.</param>
		Image GetAccountIcon(string username)
		{
			var address = new MailAddress(username);
            string host = address.Host;
            
			if(host.Contains("microsoft") || host.Contains("hotmail"))
			{
				return Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.microsoft.png");
			}

			return null;
		}

        /// <summary>
        /// Loads the workspaces.
        /// </summary>
        void LoadWorkspaces()
        {
            if (_projectCollection != null)
            {    
				var workspaces = _projectCollection.GetLocalWorkspaces();

                _workspaces.Clear();
                _workspaceStore.Clear();
				_workspaceComboBox.SelectionChanged -= OnChangeWorkspace;

                _workspaces.AddRange(workspaces);

				int i = 0;
                int activeWorkspaceRow = 0;

				if (_workspaces.Any())
				{
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
				}
				else
				{
					var workspaceRow = _workspaceStore.AddRow();
					_workspaceStore.SetValue(workspaceRow, _workspaceNameField, "There is no local Workspace");

					_workspaces.Add(new WorkspaceData());
				}
            
                var customWorkspaceRow = _workspaceStore.AddRow();
                _workspaceStore.SetValue(customWorkspaceRow, _workspaceNameField, "Create Workspace...");
                _workspaceStore.SetValue(customWorkspaceRow, _workspaceObjectField, 1);

                customWorkspaceRow = _workspaceStore.AddRow();
                _workspaceStore.SetValue(customWorkspaceRow, _workspaceNameField, "Manage Workspaces...");
                _workspaceStore.SetValue(customWorkspaceRow, _workspaceObjectField, 2);

				_workspaceComboBox.SelectionChanged += OnChangeWorkspace;
                
				if (!activeWorkspaceRow.Equals(0))
					_workspaceComboBox.SelectedIndex = activeWorkspaceRow;
				else
					_workspaceComboBox.SelectedIndex = 0;

				LoadCurrentWorkspace();
            }
        }

        /// <summary>
        /// Loads the project collection.
        /// </summary>
        void LoadProjectCollection()
        {
			var row = _accountComboBox.SelectedIndex;

            if (row == -1)
                return;

			var selectedItem = _accountStore.GetValue(row, _accountObjectField);
            

            _server = (TeamFoundationServer)selectedItem;
            var projectCollections = _server.ProjectCollections;

            if (projectCollections.Any())
            {
                _projectCollectionComboBox.Items.Clear();
                foreach (var projectCollection in projectCollections)
                {
                    _projectCollectionComboBox.Items.Add(projectCollection, projectCollection.Name);
                }

                _projectCollectionComboBox.SelectedItem = projectCollections.FirstOrDefault();
            }
        }

		/// <summary>
		/// Loads the current workspace.
		/// </summary>
		void LoadCurrentWorkspace()
		{
			var row = _workspaceComboBox.SelectedIndex;

			if (row == -1)
				return;

			var selectedItem = _workspaceStore.GetValue(row, _workspaceObjectField);

			if (selectedItem is WorkspaceData workspaceData)
			{
				_currentWorkspace = DependencyContainer.GetWorkspace(workspaceData, _projectCollection);
			}
		}

        /// <summary>
        /// Reset the projects information.
        /// </summary>
        void ClearProjects()
		{
			_projectsStore.Clear();
			_filesStore.Clear();
		}

        /// <summary>
        /// Loads the projects.
        /// </summary>
        void LoadProjects()
        {
			_projectsSpinner.Visible = true;

            _worker = Task.Factory.StartNew(delegate
            {
                if (!_workerCancel.Token.IsCancellationRequested)
                {
                    _server.LoadStructure();
                    var selectedColletion = _server.ProjectCollections.FirstOrDefault();

                    Application.Invoke(() =>
                    {
						ClearProjects();

						if (selectedColletion != null)
						{
							int count = 0;
							foreach (var project in selectedColletion.Projects.Where(p => !IsSourceControlGitEnabled(p.ProjectDetails)))
							{
								var row = _projectsStore.AddRow();
								_projectsStore.SetValue(row, _projectType, GetProjectTypeImage(project.ProjectDetails));
								_projectsStore.SetValue(row, _projectName, project.Name);
								_projectsStore.SetValue(row, _projectItem, project);
                               
								if (selectedColletion.Projects.Any() && _projectsListView != null)
								{
									_projectsListView.SelectRow(0);
								}

								count++;
							}
						}

						_projectsSpinner.Visible = false;
                    });
                }
            }, _workerCancel.Token, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Loads the selected project folders.
        /// </summary>
        /// <param name="path">Path.</param>
        void LoadFolders(string path)
        {
            _filesStore.Clear();
			_selectedItems.Clear();

            if (_currentWorkspace == null)
                return;

            if (string.IsNullOrEmpty(path))
                return;

            var items = _currentWorkspace.GetItems(new[] { new ItemSpec(path, RecursionType.Full) },                                              
                                                   VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder, false);

            if (items.Any())
            {
                var root = ItemSetToHierarchItemConverter.Convert(items);
                var rootNode = _filesStore.AddNode();

                var serverName = string.Equals(_projectCollection.Server.Name, _projectCollection.Server.Uri.OriginalString, StringComparison.OrdinalIgnoreCase)
                    ? _projectCollection.Server.Uri.Host
                    : _projectCollection.Server.Name;

                var rootName = string.Format("{0}\\{1}", serverName, _projectCollection.Name);

                rootNode.SetValue(_isCheckedField, true);
                rootNode.SetValue(_fileName, rootName);
                rootNode.SetValue(_baseItem, root.Item);
             
				_selectedItems?.Add(root.Item);
                
                foreach (var child in root.Children)
                {
                    var childNode = _filesStore.AddNode(rootNode.CurrentPosition);
                 
                    childNode.SetValue(_isCheckedField, false);
                    childNode.SetValue(_fileName, child.Name);
                    childNode.SetValue(_baseItem, child.Item);
                }

                _filesView.ExpandAll();
            }
        }

        /// <summary>
        /// Determine if the project use git as source control.
        /// </summary>
        /// <returns><c>true</c>, if source control git enabled was ised, <c>false</c> otherwise.</returns>
        /// <param name="projectDetails">Project details.</param>
        bool IsSourceControlGitEnabled(ProjectDetails projectDetails)
        {
            if (projectDetails.Details.Any(p => p.Name == "System.SourceControlGitEnabled"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
		/// Gets the project type image (Git or VSTS).
        /// </summary>
        /// <returns>The project type image.</returns>
        /// <param name="projectDetails">Project details.</param>
        Image GetProjectTypeImage(ProjectDetails projectDetails)
        {
            if (IsSourceControlGitEnabled(projectDetails))
            {
                return Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.Git.png").WithSize(16, 16);
            }

            return Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.VSTS.png").WithSize(16, 16);
        }
	}
}