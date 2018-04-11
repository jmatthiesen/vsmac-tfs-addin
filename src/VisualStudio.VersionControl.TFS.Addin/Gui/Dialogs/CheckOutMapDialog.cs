// CheckOutDialog.cs
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class CheckOutMapDialog : Dialog
    {
        Label _titleLabel;   
        ComboBox _accountComboBox;
        ComboBox _projectCollectionComboBox;
        TreeView _projectsTreeView;
        DataField<Image> _projectType;
        DataField<string> _projectName;
        DataField<ProjectInfo> _projectItem;
        TreeStore _projectsStore; 
        TreeView _filesView;
        DataField<bool> _isCheckedField;
        DataField<string> _fileName;
        DataField<BaseItem> _baseItem;
        TreeStore _filesStore;
        Button _refreshButton;
        ComboBox _workspaceComboBox;
        TextEntry _localPathEntry;
        Button _browseButton;

        TeamFoundationServer _server;
        ProjectCollection _projectCollection;

        IWorkspaceService _currentWorkspace;
        TeamFoundationServerVersionControlService _versionControlService;

        internal CheckOutMapDialog(TeamFoundationServerVersionControlService versionControlService)
        {
            Init(versionControlService);
            BuildGui();
            AttachEvents();

            using (var progress = new MessageDialogProgressMonitor(true, false, false))
            {
                progress.BeginTask("Loading...", 2);
                LoadAccounts();
                LoadProjectCollection();
                progress.Step(1);
                LoadWorkspaces();
                LoadProjects();
                progress.EndTask();
            }
        }

        void Init(TeamFoundationServerVersionControlService versionControlService)
        {
            _versionControlService = versionControlService;

            _titleLabel = new Label(GettextCatalog.GetString("Your Hosted Repositories"))
            {
                Margin = new WidgetSpacing(0, 0, 0, 12)
            };
            _accountComboBox = new ComboBox
            {
                MinWidth = 500
            };
            _projectCollectionComboBox = new ComboBox
            {
                MinWidth = 500
            };
            _projectsTreeView = new TreeView();
            _projectType = new DataField<Image>();
            _projectName = new DataField<string>();
            _projectItem = new DataField<ProjectInfo>();
            _projectsStore = new TreeStore(_projectType, _projectName, _projectItem);

            _filesView = new TreeView
            {
                ExpandHorizontal = true,
                ExpandVertical = true
            };

            _isCheckedField = new DataField<bool>();
            _fileName = new DataField<string>();
            _baseItem = new DataField<BaseItem>();
            _filesStore = new TreeStore(_isCheckedField, _fileName, _baseItem);

            _refreshButton = new Button(GettextCatalog.GetString("Refresh"));
            _workspaceComboBox = new ComboBox
            {
                MinWidth = 150
            };
            _localPathEntry = new TextEntry
            { 
                ReadOnly = true,
                MinWidth = 350
            };
            _localPathEntry.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _browseButton = new Button(GettextCatalog.GetString("Browse..."))
            {
                HorizontalPlacement = WidgetPlacement.End
            };
        } 

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Checkout from VSTS or TFS Repository");

            VBox content = new VBox
            {
                Margin = 12,
                MinWidth = 700
            };

            content.PackStart(_titleLabel);

            VBox selectorBox = new VBox();

            HBox accountBox = new HBox();
            Label accountLabel = new Label(GettextCatalog.GetString("Account:"))
            {
                WidthRequest = 60,
                HorizontalPlacement = WidgetPlacement.End,
                Margin = new WidgetSpacing(240, 0, 0, 0)
            };
            accountBox.PackStart(accountLabel);
            accountBox.PackEnd(_accountComboBox);
            selectorBox.PackStart(accountBox);

            HBox collectionBox = new HBox();
            Label collectionLabel = new Label(GettextCatalog.GetString("Collection:"))
            {
                WidthRequest = 60,
                HorizontalPlacement = WidgetPlacement.End,
                Margin = new WidgetSpacing(240, 0, 0, 0)
            };
            collectionBox.PackStart(collectionLabel);
            collectionBox.PackEnd(_projectCollectionComboBox);

            selectorBox.PackEnd(collectionBox);
            content.PackStart(selectorBox);

            HBox mainBox = new HBox();
            VBox listViewBox = new VBox
            {
                WidthRequest = 300
            };
            listViewBox.PackStart(new Label(GettextCatalog.GetString("Team Project")));
            _projectsTreeView.HeadersVisible = false;
            _projectsTreeView.MinHeight = 300;
            var projectTypeColumn = new ListViewColumn("Type");
            projectTypeColumn.Views.Add(new ImageCellView(_projectType));
            _projectsTreeView.Columns.Add(projectTypeColumn);
            _projectsTreeView.Columns.Add("Name", _projectName);
            _projectsTreeView.DataSource = _projectsStore;
            listViewBox.PackStart(_projectsTreeView);
            mainBox.PackStart(listViewBox);

            VBox treeViewBox = new VBox();
            _filesView.HeadersVisible = false;
            _filesView.MinHeight = 300;
            _filesView.WidthRequest = 500;
            treeViewBox.PackStart(new Label(GettextCatalog.GetString("Directory for Mapping")));
            var checkView = new CheckBoxCellView(_isCheckedField)
            {
                Editable = true
            };
            _filesView.Columns.Add("", checkView);
            _filesView.Columns.Add("Name", _fileName);
            _filesView.DataSource =_filesStore;
            treeViewBox.PackStart(_filesView);
            mainBox.PackStart(treeViewBox);

            content.PackStart(mainBox);

            FrameBox refreshBox = new FrameBox();
            _refreshButton.HorizontalPlacement = WidgetPlacement.End;
            refreshBox.Content = _refreshButton;
            content.PackStart(refreshBox);

            HBox workspaceBox = new HBox();
            workspaceBox.PackStart(new Label(GettextCatalog.GetString("Workspace:")));
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

            Button checkoutButton = new Button(GettextCatalog.GetString("Checkout"))
            {
                BackgroundColor = Colors.SkyBlue,
                MinWidth = GuiSettings.ButtonWidth
            };
            checkoutButton.HorizontalPlacement = WidgetPlacement.End;

            buttonBox.PackStart(cancelButton);
            buttonBox.PackEnd(checkoutButton);

            content.PackStart(buttonBox);

            Content = content;
            Resizable = false;
        }

        void AttachEvents()
        {
            _accountComboBox.SelectionChanged += OnChangeAccount;
            _projectCollectionComboBox.SelectionChanged += OnChangeProjectCollection;
            _workspaceComboBox.SelectionChanged += OnChangeWorkspace;
            _projectsTreeView.SelectionChanged += OnChangeProject;
            _browseButton.Clicked += OnBrowse;
            _refreshButton.Clicked += OnRefresh;
        }

        void OnChangeAccount(object sender, EventArgs args)
        {
            _server = (TeamFoundationServer)_accountComboBox.SelectedItem;
        }

        void OnChangeProjectCollection(object sender, EventArgs args)
        {
            _projectCollection = (ProjectCollection)_projectCollectionComboBox.SelectedItem;
        }

        void OnChangeWorkspace(object sender, EventArgs args)
        {
            var selectedItem = _workspaceComboBox.SelectedItem;

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
                }
            }
            else
            {
                var workspaceData = (WorkspaceData)_workspaceComboBox.SelectedItem;

                if (workspaceData != null)
                {
                    _currentWorkspace = DependencyContainer.GetWorkspace(workspaceData, _projectCollection);

                    if (_currentWorkspace.Data.WorkingFolders.Any())
                    {
                        var localItem = _currentWorkspace.Data.WorkingFolders.FirstOrDefault().LocalItem;
                        _localPathEntry.Text = localItem.GetDirectory().ToString();
                    }
                }
            }
        }

        void OnChangeProject(object sender, EventArgs args)
        {
            var iterPos = _projectsTreeView.SelectedRow;
            var navigator = _projectsStore.GetNavigatorAt(iterPos);

            var projectItem = navigator.GetValue(_projectItem);

            if (projectItem != null)
            {
                LoadFolders(string.Format("$/{0}", projectItem.Name));
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
            using (var progress = new MessageDialogProgressMonitor(true, false, false))
            {
                progress.BeginTask("Loading...", 2);
                LoadAccounts();
                LoadProjectCollection();
                progress.Step(1);
                LoadWorkspaces();
                LoadProjects();
                progress.EndTask();
            }
        }

        void LoadAccounts()
        {
            var servers = _versionControlService.Servers;

            if (servers.Any())
            {
                _accountComboBox.Items.Clear();
                foreach (var server in servers)
                {
                    _accountComboBox.Items.Add(server, server.UserName);
                }

                _accountComboBox.SelectedItem = servers.FirstOrDefault();
            }
        }

        void LoadWorkspaces()
        {
            if (_projectCollection != null)
            {
                var localWorkspaces = _projectCollection.GetLocalWorkspaces();

                _workspaceComboBox.Items.Clear();
                foreach (var workspace in localWorkspaces)
                {
                    _workspaceComboBox.Items.Add(workspace, string.Format("{0} {1}", workspace.Name, workspace.WorkingFolders.FirstOrDefault().ServerItem.ItemName));
                }

                _workspaceComboBox.Items.Add(1, "Create Workspace...");
                _workspaceComboBox.Items.Add(2, "Manage Workspaces...");

                if (localWorkspaces.Any(w => w.Name == _projectCollection.ActiveWorkspaceName))
                {
                    _workspaceComboBox.SelectedItem = localWorkspaces.First(w => w.Name == _projectCollection.ActiveWorkspaceName);
                }

                LoadCurrentWorkspace();
            }
        }

        void LoadProjectCollection()
        {
            _server = (TeamFoundationServer)_accountComboBox.SelectedItem;
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

        void LoadCurrentWorkspace()
        {
            var workspaceData = (WorkspaceData)_workspaceComboBox.SelectedItem;
            _currentWorkspace = DependencyContainer.GetWorkspace(workspaceData, _projectCollection);
        }

        void LoadProjects()
        {
            _server.LoadStructure();
            var selectedColletion = _server.ProjectCollections.FirstOrDefault();

            _projectsStore.Clear();

            int count = 0;
            foreach (var project in selectedColletion.Projects.Where(p => !IsSourceControlGitEnabled(p.ProjectDetails)))
            {
                var node = _projectsStore.AddNode();
                node.SetValue(_projectType, GetProjectTypeImage(project.ProjectDetails));
                node.SetValue(_projectName, project.Name);
                node.SetValue(_projectItem, project);

                if (count == 0)
                {
                    _projectsTreeView.SelectRow(node.CurrentPosition);
                }

                count++;
            }
        }

        void LoadFolders(string path = "")
        {
            _filesStore.Clear();

            if (_currentWorkspace == null)
                return;

            if(string.IsNullOrEmpty(path))
            {
                path = RepositoryPath.RootPath;
            }

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

        bool IsSourceControlGitEnabled(ProjectDetails projectDetails)
        {
            if (projectDetails.Details.Any(p => p.Name == "System.SourceControlGitEnabled"))
            {
                return true;
            }

            return false;
        }

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