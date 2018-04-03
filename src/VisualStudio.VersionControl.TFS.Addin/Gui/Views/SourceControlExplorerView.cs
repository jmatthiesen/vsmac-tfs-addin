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
using Autofac;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.ProgressMonitoring;
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

        Gtk.VBox _view;
        Gtk.Button _manageButton;
        Gtk.Button _refreshButton;
        Gtk.ComboBox _workspaceComboBox;
        Gtk.Label _workspaceLabel;
        Gtk.Label _noWorkspacesLabel;
        Gtk.ListStore _workspaceStore;
        Gtk.TreeView _treeView;
        Gtk.TreeStore _treeStore;
        Gtk.Label _localFolder;
        Gtk.TreeView _listView;
        Gtk.ListStore _listStore;

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

            using (var progress = new MessageDialogProgressMonitor(true, false, false))
            {
                progress.BeginTask("Loading...", 2);
                GetWorkspaces();
                progress.Step(1);
                GetData();
                ExpandPath(RepositoryPath.RootPath);
                progress.EndTask();
            }
        }

        #endregion

        #region Properties

        public override Control Control => _view;

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

        void Init()
        {
            _workspaces = new List<WorkspaceData>();

            _view = new Gtk.VBox();
            _localFolder = new Gtk.Label();
            _manageButton = new Gtk.Button(GettextCatalog.GetString("Manage"));
            _refreshButton = new Gtk.Button(GettextCatalog.GetString("Refresh"));

            _workspaceComboBox = new Gtk.ComboBox();
            _workspaceStore = new Gtk.ListStore(typeof(WorkspaceService), typeof(string));

            _workspaceLabel = new Gtk.Label(GettextCatalog.GetString("Workspace") + ":");
            _noWorkspacesLabel = new Gtk.Label(GettextCatalog.GetString("No Workspaces"));

            _workspaceLabel.Visible = false;
            _workspaceComboBox.Visible = false;

            _treeView = new Gtk.TreeView();
            _treeStore = new Gtk.TreeStore(typeof(BaseItem), typeof(Gdk.Pixbuf), typeof(string));

            _listView = new Gtk.TreeView();
            _listStore = new Gtk.ListStore(typeof(ExtendedItem), typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
    
            _versionControlService = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();
        }

        void BuildGui()
        {
            Gtk.HBox headerBox = new Gtk.HBox();

            headerBox.PackStart(_workspaceLabel, false, false, 0);
            headerBox.PackStart(_noWorkspacesLabel, false, false, 0);

            _workspaceComboBox.Model = _workspaceStore;
            var workspaceTextRenderer = new CellRendererText();
            _workspaceComboBox.PackStart(workspaceTextRenderer, true);
            _workspaceComboBox.SetAttributes(workspaceTextRenderer, "text", 1);

            headerBox.PackStart(_workspaceComboBox, false, false, 0);

            headerBox.PackStart(_manageButton, false, false, 0);
            headerBox.PackStart(_refreshButton, false, false, 0);
            _view.PackStart(headerBox, false, false, 0);

            Gtk.HPaned mainBox = new Gtk.HPaned();

            Gtk.VBox treeViewBox = new Gtk.VBox();

            TreeViewColumn treeColumn = new TreeViewColumn();
            treeColumn.Title = "Folders";
            var repoImageRenderer = new CellRendererPixbuf();
            treeColumn.PackStart(repoImageRenderer, false);
            treeColumn.SetAttributes(repoImageRenderer, "pixbuf", 1);
            var folderTextRenderer = new CellRendererText();
            treeColumn.PackStart(folderTextRenderer, true);
            treeColumn.SetAttributes(folderTextRenderer, "text", 2);
            _treeView.AppendColumn(treeColumn);

            treeViewBox.WidthRequest = 300;

            ScrolledWindow scrollContainer = new ScrolledWindow();
            scrollContainer.Add(_treeView);
            treeViewBox.PackStart(scrollContainer, true, true, 0);
            mainBox.Pack1(treeViewBox, false, false);

            Gtk.VBox rightBox = new Gtk.VBox();
            Gtk.HBox headerRightBox = new Gtk.HBox();

            headerRightBox.PackStart(new Gtk.Label(GettextCatalog.GetString("Local Path") + ":"), false, false, 0);
            rightBox.PackStart(headerRightBox, false, false, 0);
            headerRightBox.PackStart(_localFolder, false, false, 0);

            var itemNameColumn = new TreeViewColumn();
            itemNameColumn.Title = "Name";
            var itemIconRenderer = new CellRendererPixbuf();
            itemNameColumn.PackStart(itemIconRenderer, false);
            itemNameColumn.SetAttributes(itemIconRenderer, "pixbuf", 1);
            var itemNameRenderer = new CellRendererText();
            itemNameColumn.PackStart(itemNameRenderer, true);
            itemNameColumn.SetAttributes(itemNameRenderer, "text", 2);
            _listView.AppendColumn(itemNameColumn);

            _listView.AppendColumn("Pending Change", new CellRendererText(), "text", 3);
            _listView.AppendColumn("User", new CellRendererText(), "text", 4);
            _listView.AppendColumn("Latest", new CellRendererText(), "text", 5);
            _listView.AppendColumn("Last Check-in", new CellRendererText(), "text", 6);

            _listView.Selection.Mode = Gtk.SelectionMode.Multiple;
            _listView.Model = _listStore;
            var listViewScollWindow = new ScrolledWindow();
            listViewScollWindow.Add(_listView);
            rightBox.PackStart(listViewScollWindow, true, true, 0);
            mainBox.Pack2(rightBox, true, true);
            _view.PackStart(mainBox, true, true, 0);

            _view.ShowAll();
        }

        void AttachEvents()
        {
            _workspaceComboBox.Changed += OnChangeActiveWorkspaces;
            _manageButton.Clicked += OnManageWorkspaces;
            _refreshButton.Clicked += OnRefresh;
            _treeView.Selection.Changed += OnFolderChanged;
            _treeView.RowActivated += OnTreeViewItemClicked;
            _treeView.ButtonPressEvent += OnTreeViewMouseClick;
            _listView.RowActivated += OnListItemClicked;
            _listView.ButtonPressEvent += OnListViewMouseClick;
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
                switch (menuType)
                {
                    case MenuType.List:
                        GetListView(item.ServerPath.ParentPath);
                        break;
                    case MenuType.Tree:
                        GetListView(item.ServerPath);
                        break;
                }
            }
        }

        void Refresh(IEnumerable<BaseItem> items)
        {
            Refresh(items.FirstOrDefault(), MenuType.List);
        }
             
        void GetWorkspaces()
        {
            try
            {
                _workspaceComboBox.Changed -= OnChangeActiveWorkspaces;
                _workspaceStore.Clear();
                _workspaces.Clear();
                _workspaces.AddRange(_projectCollection.GetLocalWorkspaces());
                TreeIter activeWorkspaceRow = TreeIter.Zero;

                foreach (var workspace in _workspaces)
                {
                    var iter = _workspaceStore.AppendValues(workspace, workspace.Name);
                    if (string.Equals(workspace.Name, _projectCollection.ActiveWorkspaceName, StringComparison.Ordinal))
                    {
                        activeWorkspaceRow = iter;
                    }
                }
                _workspaceComboBox.Changed += OnChangeActiveWorkspaces;

                if (_workspaces.Count > 0)
                {
                    if (!activeWorkspaceRow.Equals(TreeIter.Zero))
                        _workspaceComboBox.SetActiveIter(activeWorkspaceRow);
                    else
                        _workspaceComboBox.Active = 0;

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
            catch
            {
                _noWorkspacesLabel.Visible = true;
                _workspaceLabel.Visible = false;
                _workspaceComboBox.Visible = false;
            }
        }

        void GetData()
        {
            _treeStore.Clear();

            if (_currentWorkspace == null)
                return;
            
            var items = _currentWorkspace.GetItems(new[] { new ItemSpec(RepositoryPath.RootPath, RecursionType.Full) }, 
                                                   VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder, false);

            var root = ItemSetToHierarchItemConverter.Convert(items);
            var node = _treeStore.AppendNode();

            var serverName = string.Equals(_projectCollection.Server.Name, _projectCollection.Server.Uri.OriginalString, StringComparison.OrdinalIgnoreCase)
                ? _projectCollection.Server.Uri.Host
                : _projectCollection.Server.Name;
            
            var rootName = string.Format("{0}\\{1}", serverName, _projectCollection.Name);
            _treeStore.SetValues(node, root.Item, ImageHelper.GetRepositoryImage(), rootName);
            AddChilds(node, root.Children);

            _treeView.Model = _treeStore;

            TreeIter firstNode;
           
            if (_treeStore.GetIterFirst(out firstNode))
            {
                _treeView.ExpandRow(_treeStore.GetPath(firstNode), false);
                _treeView.Selection.SelectIter(firstNode);
            }
        }

        void AddChilds(TreeIter node, List<HierarchyItem> children)
        {
            _treeLevel++;
            foreach (var child in children)
            {
                var childNode = _treeStore.AppendNode(node);
                _treeStore.SetValue(childNode, 0, child.Item);
                _treeStore.SetValue(childNode, 2, child.Name);
               
                if (_treeLevel == 1)
                    _treeStore.SetValue(childNode, 1, ImageHelper.GetRepositoryImage());
                else
                    _treeStore.SetValue(childNode, 1, ImageHelper.GetItemImage(ItemType.Folder));
               
                AddChilds(childNode, child.Children);
            }
            _treeLevel--;
        }

        void GetListView(string serverPath)
        {
            _listStore.Clear();
            var itemSet = _currentWorkspace.GetExtendedItems(new[] { new ItemSpec(serverPath, RecursionType.OneLevel) }, DeletedState.NonDeleted, ItemType.Any);
            foreach (var item in itemSet.Skip(1).OrderBy(i => i.ItemType).ThenBy(i => i.TargetServerItem))
            {
                var row = _listStore.Append();
                _listStore.SetValue(row, 0, item);
                _listStore.SetValue(row, 1, ImageHelper.GetItemImage(item.ItemType));
                _listStore.SetValue(row, 2, item.ServerPath.ItemName);

                if (_currentWorkspace != null)
                {
                    if (item.ChangeType != ChangeType.None && !item.HasOtherPendingChange)
                    {
                        _listStore.SetValue(row, 3, item.ChangeType.ToString());
                        _listStore.SetValue(row, 4, _currentWorkspace.Data.Owner);
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
                        _listStore.SetValue(row, 3, string.Join(", ", changeNames));
                        _listStore.SetValue(row, 4, string.Join(", ", userNames));
                    }
                }

                if (!IsMapped(item.ServerPath))
                {
                    _listStore.SetValue(row, 5, "Not mapped");
                }
                else
                {
                    if (!item.IsInWorkspace)     
                    {
                        _listStore.SetValue(row, 5, "Not downloaded");
                    }
                    else
                    {
                        _listStore.SetValue(row, 5, item.IsLatest ? "Yes" : "No");
                    }
                }

                _listStore.SetValue(row, 6, item.CheckinDate.ToString("yyyy-MM-dd HH:mm"));
            }
        }

        void OnFolderChanged(object sender, EventArgs e)
        {
            TreeIter iter;
            if (!_treeView.Selection.GetSelected(out iter))
                return;

            var item = (BaseItem)_treeStore.GetValue(iter, 0);
            GetListView(item.ServerPath);
            ShowMappingPath(item.ServerPath);
        }

        void OnTreeViewItemClicked(object o, RowActivatedArgs args)
        {
            var isExpanded = _treeView.GetRowExpanded(args.Path);

            if (isExpanded)
                _treeView.CollapseRow(args.Path);
            else
                _treeView.ExpandRow(args.Path, false);
        }

        void OnListItemClicked(object sender, RowActivatedArgs e)
        {
            TreeIter iter;

            if (!_listStore.GetIter(out iter, e.Path))
                return;

            var item = (ExtendedItem)_listStore.GetValue(iter, 0);

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
        void OnListViewMouseClick(object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 3 && _listView.Selection.GetSelectedRows().Any())
            {
                var menu = BuildListViewPopupMenu();

                if (menu.Children.Length > 0)
                    menu.Popup();
                
                args.RetVal = true;
            }
        }

        [GLib.ConnectBefore]
        void OnTreeViewMouseClick(object o, ButtonPressEventArgs args)
        {
            TreeIter iter;
            if (args.Event.Button == 3 && _treeView.Selection.GetSelected(out iter))
            {
                var item = (BaseItem)_treeStore.GetValue(iter, 0);
                var menu = BuildTreePopupMenu(item);
              
                if (menu.Children.Length > 0)
                    menu.Popup();
                
                args.RetVal = true;
            }
        }

        void OnChangeActiveWorkspaces(object sender, EventArgs ev)
        {
            TreeIter workspaceIter;
            if (_workspaceComboBox.GetActiveIter(out workspaceIter))
            {
                var workspaceData = (WorkspaceData)_workspaceStore.GetValue(workspaceIter, 0);
                _currentWorkspace = DependencyContainer.GetWorkspace(workspaceData, _projectCollection);
                _versionControlService.SetActiveWorkspace(_projectCollection, workspaceData.Name);

                TreeIter treeIter;
                if (_treeView.Selection.GetSelected(out treeIter))
                {
                    var currentItem = (BaseItem)_treeStore.GetValue(treeIter, 0);
                    ShowMappingPath(currentItem.ServerPath);
                    GetListView(currentItem.ServerPath);
                }
            }
            else
            {
                _versionControlService.SetActiveWorkspace(_projectCollection, string.Empty);
            }
        }

        void ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            TreeIter iter = TreeIter.Zero;
            _treeStore.Foreach((m, p, i) =>
            {
                var item = ((BaseItem)m.GetValue(i, 0));
              
                if (string.Equals(item.ServerPath, path, StringComparison.OrdinalIgnoreCase))
                {
                    iter = i;
                    return true;
                }

                return false;
            });

            if (iter.Equals(TreeIter.Zero))
                return;

            _treeView.CollapseAll();
            _treeView.ExpandToPath(_treeStore.GetPath(iter));
            _treeView.Selection.SelectIter(iter);
        }

        void FindItem(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            
            TreeIter iter = TreeIter.Zero;
            _listStore.Foreach((model, path, it) =>
            {
                var item = ((BaseItem)model.GetValue(it, 0));
               
                if (string.Equals(item.ServerPath.ItemName, name, StringComparison.OrdinalIgnoreCase))
                {
                    iter = it;
                    return true;
                }

                return false;
            });

            if (iter.Equals(TreeIter.Zero))
                return;
            
            _listView.Selection.SelectIter(iter);
            var treePath = _listStore.GetPath(iter);
            _listView.ScrollToCell(treePath, _listView.Columns[0], false, 0, 0);
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
                    GetWorkspaces();
                }
            }
        }

        void OnRefresh(object sender, EventArgs e)
        {
            TreeIter iter;
            RepositoryPath selectedPath = null;

            if (_treeView.Selection.GetSelected(out iter))
                selectedPath = ((BaseItem)_treeStore.GetValue(iter, 0)).ServerPath;
            
            GetData();

            if (selectedPath != null)
                ExpandPath(selectedPath);
        }

        #region Popup Menu

        enum MenuType
        {
            Tree,
            List
        }

        Gtk.Menu BuildListViewPopupMenu()
        {
            Gtk.Menu menu = new Gtk.Menu();
            var items = new List<ExtendedItem>();

            foreach (var path in _listView.Selection.GetSelectedRows())
            {
                TreeIter iter;
                _listStore.GetIter(out iter, path);
                items.Add((ExtendedItem)_listStore.GetValue(iter, 0));
            }

            if (items.All(i => IsMapped(i.ServerPath)))
            {
                foreach (var item in GetGroup(items))
                {
                    menu.Add(item);
                }

                menu.Add(new Gtk.SeparatorMenuItem());

                foreach (var item in EditGroup(items))
                {
                    menu.Add(item);
                }

                if (items.Count == 1)
                {
                    foreach (var menuItem in ForlderMenuItems(items[0], MenuType.List))
                    {
                        menu.Add(menuItem);
                    }
                }
            }
            else
            {
                menu.Add(NotMappedMenu(items, MenuType.List));
            }

            menu.ShowAll();

            return menu;
        }

        IEnumerable<Gtk.MenuItem> GetGroup(List<ExtendedItem> items)
        {
            Gtk.MenuItem getLatestVersionItem = new Gtk.MenuItem(GettextCatalog.GetString("Get Latest Version"));
            getLatestVersionItem.Activated += (sender, e) => GetLatestVersion(items);

            yield return getLatestVersionItem;

            Gtk.MenuItem forceGetLatestVersionItem = new Gtk.MenuItem(GettextCatalog.GetString("Get Specific Version"));
            forceGetLatestVersionItem.Activated += (sender, e) => ForceGetLatestVersion(items);

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

            using (var progress = VersionControlService.GetProgressMonitor("Get", VersionControlOperationType.Pull))
            {
                var option = GetOptions.None;
                progress.Log.WriteLine("Start downloading items. GetOption: " + option);

                foreach (var request in requests)
                {
                    progress.Log.WriteLine(request);
                }

                _currentWorkspace.Get(requests, option);
                progress.ReportSuccess("Finish Downloading.");
            }

            Refresh(items);
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

        IEnumerable<Gtk.MenuItem> EditGroup(List<ExtendedItem> items)
        {
            //Check Out
            var checkOutItems = items.Where(i => i.ChangeType == ChangeType.None || i.ChangeType == ChangeType.Lock || i.ItemType == ItemType.Folder).ToList();
        
            if (checkOutItems.Any())
            {
                Gtk.MenuItem checkOutItem = new Gtk.MenuItem(GettextCatalog.GetString("Check out items"));
              
                checkOutItem.Activated += (sender, e) =>
                {
                    using (var dialog = new CheckOutDialog(checkOutItems, _currentWorkspace))
                    {
                        if (dialog.Run() == Command.Ok)
                        {
                            var itemsToCheckOut = dialog.SelectedItems;
                            CheckOut(itemsToCheckOut);
                        }
                    }

                    FireFilesChanged(checkOutItems);
                    Refresh(items);
                };

                yield return checkOutItem;
            }

            // Check In
            var checkInItems = items.Where(i => !i.ChangeType.HasFlag(ChangeType.None)).ToList();

            if (checkInItems.Any())
            {
                Gtk.MenuItem checkinItem = new Gtk.MenuItem(GettextCatalog.GetString("Check In"));

                checkinItem.Activated += (sender, e) =>
                {
                    using (var dialog = new CheckInDialog(checkOutItems, _currentWorkspace))
                    {
                        if (dialog.Run() == Command.Ok)
                        {
                            using (var progress = new MessageDialogProgressMonitor(true, false, false))
                            {
                                progress.BeginTask("Check In", 1);

                                var result = _currentWorkspace.CheckIn(dialog.SelectedChanges, dialog.Comment, dialog.SelectedWorkItems);

                                foreach (var failure in result.Failures.Where(f => f.SeverityType == SeverityType.Error))
                                {
                                    progress.ReportError(failure.Code, new Exception(failure.Message));
                                }

                                progress.EndTask();
                                progress.ReportSuccess("Finish Check In");
                            }
                        }
                    }

                    FireFilesChanged(checkInItems);
                    Refresh(items);
                };

                yield return checkinItem;
            }

            //Lock
            var lockItems = items.Where(i => !i.IsLocked).ToList();
         
            if (lockItems.Any())
            {
                Gtk.MenuItem lockItem = new Gtk.MenuItem(GettextCatalog.GetString("Lock"));
               
                lockItem.Activated += (sender, e) =>
                {
                    var itemsToLock = items;
                    var lockLevel = LockLevel.CheckOut;

                    using (var progress = new MessageDialogProgressMonitor(true, false, true))
                    {
                        progress.BeginTask("Lock Files", itemsToLock.Count);
                        _currentWorkspace.LockItems(itemsToLock.Select(i => i.ServerPath), lockLevel);
                        progress.EndTask();
                        progress.ReportSuccess("Finish locking.");
                    }

                    FireFilesChanged(lockItems);
                    Refresh(items);
                };

                yield return lockItem;
            }

            //UnLock
            var unLockItems = items.Where(i => i.IsLocked && !i.HasOtherPendingChange).ToList();

            if (unLockItems.Any())
            {
                Gtk.MenuItem unLockItem = new Gtk.MenuItem(GettextCatalog.GetString("UnLock"));
              
                unLockItem.Activated += (sender, e) =>
                {
                    _currentWorkspace.UnLockItems(unLockItems.Select(i => i.ServerPath));
                    FireFilesChanged(unLockItems);
                    Refresh(items);
                };

                yield return unLockItem;
            }

            //Rename
            var ableToRename = items.FirstOrDefault(i => !i.ChangeType.HasFlag(ChangeType.Delete));
          
            if (ableToRename != null)
            {
                Gtk.MenuItem renameItem = new Gtk.MenuItem(GettextCatalog.GetString("Rename"));
               
                renameItem.Activated += (sender, e) =>
                {
                    using (var dialog = new RenameDialog(ableToRename))
                    {
                        if (dialog.Run() == Command.Ok)
                        {
                            ICollection<Failure> failures;

                            _currentWorkspace.PendRename(ableToRename.LocalPath, dialog.NewPath, out failures);

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
                Gtk.MenuItem deleteItem = new Gtk.MenuItem(GettextCatalog.GetString("Delete"));
               
                deleteItem.Activated += (sender, e) =>
                {
                    if (MessageService.Confirm(GettextCatalog.GetString("Are you sure you want to delete selected files?"), AlertButton.Yes))
                    {
                        ICollection<Failure> failures;
                        _currentWorkspace.PendDelete(items.Select(x => x.LocalPath), RecursionType.Full, false, out failures);

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
                Gtk.MenuItem undoItem = new Gtk.MenuItem(GettextCatalog.GetString("Undo Changes"));
             
                undoItem.Activated += (sender, e) =>
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

                            _currentWorkspace.Undo(itemSpecs);

                            FireFilesChanged(undoItems);
                            Refresh(items);
                        }
                    }
                };

                yield return undoItem;
            }
        }

        void CheckOut(List<ExtendedItem> itemsToCheckOut)
        {
            using (var progress = VersionControlService.GetProgressMonitor("Check Out", VersionControlOperationType.Pull))
            {
                progress.BeginTask("Check Out", itemsToCheckOut.Count);

                foreach (var item in itemsToCheckOut)
                {
                    var path = item.IsInWorkspace ? item.LocalPath : _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);
                    _currentWorkspace.Get(new GetRequest(item.ServerPath, RecursionType.Full, VersionSpec.Latest), GetOptions.None);
                    progress.Log.WriteLine("Check out item: " + item.ServerPath);

                    ICollection<Failure> failures;
                    _currentWorkspace.PendEdit(path.ToEnumerable(), RecursionType.Full, LockLevel.CheckOut, out failures);

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
        }

        IEnumerable<Gtk.MenuItem> ForlderMenuItems(BaseItem item, MenuType menuType)
        {
            if (item.ItemType != ItemType.Folder)
                yield break;
            
            yield return CreateAddFileMenuItem(item, menuType);
            yield return new Gtk.SeparatorMenuItem();
            yield return CreateOpenFolderMenuItem(item);
        }


        Gtk.MenuItem CreateAddFileMenuItem(BaseItem item, MenuType menuType)
        {
            Gtk.MenuItem addItem = new Gtk.MenuItem(GettextCatalog.GetString("Add New Item"));
           
            addItem.Activated += (sender, e) =>
            {
                var path = _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);

                using (OpenFileDialog openFileDialog = new OpenFileDialog("Browse For File"))
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

                        ICollection<Failure> failures;
                        _currentWorkspace.PendAdd(files, false, out failures);

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
                                    using (var progress = new MessageDialogProgressMonitor(true, false, false))
                                    {
                                        progress.BeginTask("Check In", 1);

                                        var result = _currentWorkspace.CheckIn(dialog.SelectedChanges, dialog.Comment, dialog.SelectedWorkItems);

                                        foreach (var failure in result.Failures.Where(f => f.SeverityType == SeverityType.Error))
                                        {
                                            progress.ReportError(failure.Code, new Exception(failure.Message));
                                        }

                                        progress.EndTask();
                                        progress.ReportSuccess("Finish Check In");
                                    }
                                }
                            }

                            Refresh(item, menuType);
                        }
                    }
                }
            };

            return addItem;
        }

        Gtk.MenuItem CreateOpenFolderMenuItem(BaseItem item)
        {
            Gtk.MenuItem openFolder = new Gtk.MenuItem(GettextCatalog.GetString("Open Folder"));
          
            openFolder.Activated += (sender, e) =>
            {
                var path = _currentWorkspace.Data.GetLocalPathForServerPath(item.ServerPath);
                DesktopService.OpenFolder(new FilePath(path));
            };

            return openFolder;
        }

        Gtk.MenuItem NotMappedMenu(IEnumerable<BaseItem> items, MenuType menuType)
        {
            Gtk.MenuItem mapItem = new Gtk.MenuItem(GettextCatalog.GetString("Map"));
            var item = items.FirstOrDefault(i => i.ItemType == ItemType.Folder);
            mapItem.Activated += (sender, e) => MapItem(item, menuType);
           
            return mapItem;
        }

        void MapItem(BaseItem item, MenuType menuType)
        {
            if (_currentWorkspace == null || item == null)
                return;
            
            using (Xwt.SelectFolderDialog folderSelect = new Xwt.SelectFolderDialog("Browse For Folder"))
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
       
        Gtk.Menu BuildTreePopupMenu(BaseItem item)
        {
            Gtk.Menu menu = new Gtk.Menu();
        
            if (item == null || item.ServerPath == RepositoryPath.RootPath)
                return menu;
            
            if (!IsMapped(item.ServerPath))
            {
                menu.Add(NotMappedMenu(item.ToEnumerable(), MenuType.Tree));
            }
            else
            {
                foreach (var menuItem in ForlderMenuItems(item, MenuType.Tree))
                {
                    menu.Add(menuItem);
                }
            }
            menu.ShowAll();
            return menu;
        }

        #endregion

        #endregion
    }
}