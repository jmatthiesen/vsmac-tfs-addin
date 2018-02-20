using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gtk;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Views
{
    public class SourceControlExplorerView : ViewContent
    {
        #region Variables

        int _treeLevel;
        ProjectCollection _projectCollection;
        List<Workspace> _workspaces;
        Workspace _currentWorkspace;

        Gtk.VBox _view;
        Gtk.Button _manageButton;
        Gtk.ComboBox _workspaceComboBox;
        Gtk.ListStore _workspaceStore;
        Gtk.TreeView _treeView;
        Gtk.TreeStore _treeStore;
        Gtk.Label _localFolder;
        Gtk.TreeView _listView;
        Gtk.ListStore _listStore;

        #endregion

        #region Constructor

        public SourceControlExplorerView(ProjectCollection projectCollection)
        {
            _projectCollection = projectCollection;
            ContentName = GettextCatalog.GetString("Source Explorer");

            Init();
            BuildGui();
            AttachEvents();

            using (var progress = new MessageDialogProgressMonitor(true, false, false))
            {
                progress.BeginTask("Loading...", 2);
                GetData();
                progress.Step(1);
                GetWorkspaces();
                ExpandPath(VersionControlPath.RootFolder);

                progress.EndTask();
            }
        }

        #endregion

        #region Properties

        public override Control Control => _view;

        #endregion

        #region Public Methods

        public static void Show(ProjectCollection projectCollection)
        {
            var sourceControlExplorerView = new SourceControlExplorerView(projectCollection);
            IdeApp.Workbench.OpenDocument(sourceControlExplorerView, true);
        }

        public static void Show(ProjectInfo project)
        {
            var sourceControlExplorerView = new SourceControlExplorerView(project.Collection);
            sourceControlExplorerView.ExpandPath(VersionControlPath.RootFolder + project.Name);
            IdeApp.Workbench.OpenDocument(sourceControlExplorerView, true);
        }

        public static void Show(ProjectCollection collection, string path, string fileName)
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
            _workspaces = new List<Workspace>();

            _view = new Gtk.VBox();
            _localFolder = new Gtk.Label();
            _manageButton = new Gtk.Button(GettextCatalog.GetString("Manage"));

            _workspaceComboBox = new Gtk.ComboBox();
            _workspaceStore = new Gtk.ListStore(typeof(Workspace), typeof(string));

            _treeView = new Gtk.TreeView();
            _treeStore = new Gtk.TreeStore(typeof(BaseItem), typeof(Gdk.Pixbuf), typeof(string));

            _listView = new Gtk.TreeView();
            _listStore = new Gtk.ListStore(typeof(ExtendedItem), typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
        }

        void BuildGui()
        {
            Gtk.HBox headerBox = new Gtk.HBox();

            headerBox.PackStart(new Gtk.Label(GettextCatalog.GetString("Workspace") + ":"), false, false, 0);

            _workspaceComboBox.Model = _workspaceStore;
            var workspaceTextRenderer = new CellRendererText();
            _workspaceComboBox.PackStart(workspaceTextRenderer, true);
            _workspaceComboBox.SetAttributes(workspaceTextRenderer, "text", 1);

            headerBox.PackStart(_workspaceComboBox, false, false, 0);

            headerBox.PackStart(_manageButton, false, false, 0);
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
            _treeView.Selection.Changed += OnFolderChanged;
            _treeView.RowActivated += OnTreeViewItemClicked;
            _listView.RowActivated += OnListItemClicked;
            _listView.ButtonPressEvent += OnListViewMouseClick;
        }

        void Refresh(List<ExtendedItem> items)
        {
            if (items.Any())
                GetListView(items[0].ServerPath.ParentPath);
        }

        void CheckOut(List<ExtendedItem> itemsToCheckOut)
        {
            using (var progress = VersionControlService.GetProgressMonitor("Check Out", VersionControlOperationType.Pull))
            {
                progress.BeginTask("Check Out", itemsToCheckOut.Count);

                foreach (var item in itemsToCheckOut)
                {
                    var path = item.IsInWorkspace ? item.LocalItem : _currentWorkspace.GetLocalPathForServerPath(item.ServerPath);
                    _currentWorkspace.Get(new GetRequest(item.ServerPath, RecursionType.Full, VersionSpec.Latest), GetOptions.None, progress);
                    progress.Log.WriteLine("Check out item: " + item.ServerPath);
                    var failures = _currentWorkspace.PendEdit(new List<FilePath> { path }, RecursionType.Full, CheckOutLockLevel.CheckOut);

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

        void MapItem(List<ExtendedItem> items)
        {
            var item = items.FirstOrDefault(i => i.ItemType == ItemType.Folder);

            if (_currentWorkspace == null || item == null)
                return;

            using (Xwt.SelectFolderDialog folderSelect = new Xwt.SelectFolderDialog("Browse For Folder"))
            {
                folderSelect.Multiselect = false;
                folderSelect.CanCreateFolders = true;

                if (folderSelect.Run())
                {
                    TeamFoundationServerClient.Instance.Map(_currentWorkspace, item.ServerPath, folderSelect.Folder);
                }

                Refresh(items);
            }
        }

        void GetWorkspaces()
        {
            string activeWorkspace = TeamFoundationServerClient.Settings.GetActiveWorkspace(_projectCollection);
            _workspaceComboBox.Changed -= OnChangeActiveWorkspaces;
            _workspaceStore.Clear();
            _workspaces.Clear();
            _workspaces.AddRange(TeamFoundationServerClient.Instance.GetWorkspaces(_projectCollection));
            TreeIter activeWorkspaceRow = TreeIter.Zero;
            foreach (var workspace in _workspaces)
            {
                var iter = _workspaceStore.AppendValues(workspace, workspace.Name);
                if (string.Equals(workspace.Name, activeWorkspace, StringComparison.Ordinal))
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
            }
        }

        void GetData()
        {
            _treeStore.Clear();
            var versionControl = _projectCollection.GetService<RepositoryService>();
            var items = versionControl.QueryItems(_currentWorkspace, new ItemSpec(VersionControlPath.RootFolder, RecursionType.Full), VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Folder, false);

            var root = ItemSetToHierarchItemConverter.Convert(items);
            var node = _treeStore.AppendNode();
            _treeStore.SetValues(node, root.Item, ImageHelper.GetRepositoryImage(), root.Name);
            AddChilds(node, root.Children);
            TreeIter firstNode;
            if (_treeStore.GetIterFirst(out firstNode))
            {
                _treeView.ExpandRow(_treeStore.GetPath(firstNode), false);
                _treeView.Selection.SelectIter(firstNode);
            }
            _treeView.Model = _treeStore;

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

            var versionControl = _projectCollection.GetService<RepositoryService>();
            var itemSet = versionControl.QueryItemsExtended(_currentWorkspace, new ItemSpec(serverPath, RecursionType.OneLevel), DeletedState.NonDeleted, ItemType.Any);
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
                        _listStore.SetValue(row, 4, _currentWorkspace.OwnerName);
                    }
                    if (item.HasOtherPendingChange)
                    {
                        var remoteChanges = this._currentWorkspace.GetPendingSets(item.SourceServerItem, RecursionType.None);
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
                ExpandPath(item.TargetServerItem);
                return;
            }

            if (item.ItemType == ItemType.File)
            {
                if (IsMapped(item.ServerPath))
                {
                    if (item.IsInWorkspace)
                    {
                        if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile(item.LocalItem))
                        {
                            IdeApp.Workspace.OpenWorkspaceItem(item.LocalItem, true);
                        }
                        else
                        {
                            IdeApp.Workbench.OpenDocument(item.LocalItem, null, true);
                        }
                    }
                    else
                    {
                        var filePath = TeamFoundationServerClient.Instance.DownloadTempItem(_currentWorkspace, _projectCollection, item);

                        if (Projects.Services.ProjectService.IsWorkspaceItemFile(filePath))
                        {
                            var parentFolder = _currentWorkspace.GetExtendedItem(item.ServerPath.ParentPath, ItemType.Folder);

                            if (parentFolder == null)
                                return;

                            TeamFoundationServerClient.Instance.GetLatestVersion(_currentWorkspace, new List<ExtendedItem> { parentFolder });
                            var futurePath = _currentWorkspace.GetLocalPathForServerPath(item.ServerPath);
                            IdeApp.Workspace.OpenWorkspaceItem(futurePath, true);
                            FileHelper.FileDelete(filePath);
                        }
                        else
                        {
                            IdeApp.Workbench.OpenDocument(filePath, null, null);
                        }
                    }
                }
                else
                {
                    var filePath = TeamFoundationServerClient.Instance.DownloadTempItem(_currentWorkspace, _projectCollection, item);
                    IdeApp.Workbench.OpenDocument(filePath, null, true);
                }
            }
        }

        [GLib.ConnectBefore]
        void OnListViewMouseClick(object o, ButtonPressEventArgs args)
        {
            if (args.Event.Button == 3 && _listView.Selection.GetSelectedRows().Any())
            {
                var menu = GetPopupMenu();

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
                var workspace = (Workspace)_workspaceStore.GetValue(workspaceIter, 0);
                _currentWorkspace = workspace;

                TeamFoundationServerClient.Settings.SetActiveWorkspace(_projectCollection, workspace.Name);

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
                TeamFoundationServerClient.Settings.SetActiveWorkspace(_projectCollection, string.Empty);
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

        bool IsMapped(string serverPath)
        {
            if (_currentWorkspace == null)
                return false;

            return _currentWorkspace.IsServerPathMapped(serverPath);
        }

        void ShowMappingPath(VersionControlPath serverPath)
        {
            if (!IsMapped(serverPath))
            {
                _localFolder.Text = GettextCatalog.GetString("Not Mapped");
                return;
            }

            var mappedFolder = _currentWorkspace.Folders.First(f => serverPath.IsChildOrEqualTo(f.ServerItem));

            if (string.Equals(serverPath, mappedFolder.ServerItem, StringComparison.Ordinal))
                _localFolder.Text = mappedFolder.LocalItem;
            else
            {
                string rest = serverPath.ChildPart(mappedFolder.ServerItem);
                _localFolder.Text = Path.Combine(mappedFolder.LocalItem, rest);
            }
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

        #region Popup Menu

        Gtk.Menu GetPopupMenu()
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
                foreach (var item in GetVersionMenu(items))
                {
                    menu.Add(item);
                }

                var editMenu = GetEditMenu(items);
                if (editMenu.Any())
                {
                    menu.Add(new Gtk.SeparatorMenuItem());
                    foreach (var item in editMenu)
                    {
                        menu.Add(item);
                    }
                }
            }
            else
            {
                foreach (var item in GetMapMenu(items))
                {
                    menu.Add(item);
                }
            }

            menu.ShowAll();

            return menu;
        }

        List<Gtk.MenuItem> GetVersionMenu(List<ExtendedItem> items)
        {
            var groupItems = new List<Gtk.MenuItem>();
            Gtk.MenuItem getLatestVersionItem = new Gtk.MenuItem(GettextCatalog.GetString("Get Latest Version"));

            getLatestVersionItem.Activated += (sender, e) =>
            {
                List<GetRequest> requests = new List<GetRequest>();

                foreach (var item in items)
                {
                    RecursionType recursion = item.ItemType == ItemType.File ? RecursionType.None : RecursionType.Full;
                    requests.Add(new GetRequest(item.ServerPath, recursion, VersionSpec.Latest));
                }

                var monitor = VersionControlService.GetProgressMonitor("Get", VersionControlOperationType.Pull);

                try
                {
                    var option = GetOptions.None;

                    monitor.Log.WriteLine("Start downloading items.");

                    TeamFoundationServerClient.Instance.Get(_currentWorkspace, requests, option, monitor);

                    monitor.ReportSuccess("Finish Downloading.");
                }
                catch (Exception ex)
                {
                    monitor.ReportError(GettextCatalog.GetString("Download failed."), ex);
                }
                finally
                {
                    monitor.Dispose();
                }

                Refresh(items);
            };

            groupItems.Add(getLatestVersionItem);

            Gtk.MenuItem forceGetLatestVersionItem = new Gtk.MenuItem(GettextCatalog.GetString("Get Specific Version"));

            forceGetLatestVersionItem.Activated += (sender, e) =>
            {
                using (var specVersionDialog = new GetSpecVersionDialog(_currentWorkspace))
                {
                    specVersionDialog.AddData(items);

                    if (specVersionDialog.Run() == Command.Ok)
                    {
                        Refresh(items);
                    }
                }
            };

            groupItems.Add(forceGetLatestVersionItem);

            return groupItems;
        }

        List<Gtk.MenuItem> GetEditMenu(List<ExtendedItem> items)
        {
            var editItems = new List<Gtk.MenuItem>();

            //Check Out
            var checkOutItems = items
                .Where(i => i.ChangeType == ChangeType.None || i.ChangeType == ChangeType.Lock || i.ItemType == ItemType.Folder)
                .ToList();

            if (checkOutItems.Any())
            {
                Gtk.MenuItem checkOutItem = new Gtk.MenuItem(GettextCatalog.GetString("Check out items"));

                checkOutItem.Activated += (sender, e) =>
                {
                    if (checkOutItems.Count > 1)
                    {
                        using (var dialog = new CheckOutDialog(checkOutItems, _currentWorkspace))
                        {
                            if (dialog.Run() == Command.Ok)
                            {
                                var itemsToCheckOut = dialog.SelectedItems;
                                CheckOut(itemsToCheckOut);
                            }
                        }
                    }
                    else
                    {
                        CheckOut(checkOutItems);
                    }

                    Refresh(items);
                };

                editItems.Add(checkOutItem);
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

                                var result = TeamFoundationServerClient.Instance.CheckIn(_currentWorkspace, dialog.SelectedChanges, dialog.Comment);
                                foreach (var failure in result.Failures.Where(f => f.SeverityType == SeverityType.Error))
                                {
                                    progress.ReportError(failure.Code, new Exception(failure.Message));
                                }

                                progress.EndTask();
                                progress.ReportSuccess("Finish Check In");
                            }
                        }
                    }

                    Refresh(items);
                };

                editItems.Add(checkinItem);
            }

            // Lock
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
                      
                        var folders = new List<string>(itemsToLock.Where(i => i.ItemType == ItemType.Folder).Select(i => (string)i.ServerPath));
                        var files = new List<string>(itemsToLock.Where(i => i.ItemType == ItemType.File).Select(i => (string)i.ServerPath));
                     
                        TeamFoundationServerClient.Instance.LockFolders(_currentWorkspace, folders, lockLevel);
                        TeamFoundationServerClient.Instance.LockFiles(_currentWorkspace, files, lockLevel);
                      
                        progress.EndTask();
                        progress.ReportSuccess("Finish locking.");
                    }

                    TeamFoundationServerFileHelper.NotifyFilesChanged(_currentWorkspace, itemsToLock);
                    Refresh(items);
                };

                editItems.Add(lockItem);
            }

            // Unlock
            var unLockItems = items.Where(i => i.IsLocked && !i.HasOtherPendingChange).ToList();
           
            if (unLockItems.Any())
            {
                Gtk.MenuItem unLockItem = new Gtk.MenuItem(GettextCatalog.GetString("UnLock"));
              
                unLockItem.Activated += (sender, e) =>
                {
                    var folders = new List<string>(unLockItems.Where(i => i.ItemType == ItemType.Folder).Select(i => (string)i.ServerPath));
                    var files = new List<string>(unLockItems.Where(i => i.ItemType == ItemType.File).Select(i => (string)i.ServerPath));
                   
                    TeamFoundationServerClient.Instance.LockFolders(_currentWorkspace, folders, LockLevel.None);
                    TeamFoundationServerClient.Instance.LockFiles(_currentWorkspace, files, LockLevel.None);

                    TeamFoundationServerFileHelper.NotifyFilesChanged(_currentWorkspace, unLockItems);
                    Refresh(items);
                };

                editItems.Add(unLockItem);
            }

            //Rename
            var itemToRename = items.FirstOrDefault(i => !i.ChangeType.HasFlag(ChangeType.Delete));
           
            if (itemToRename != null)
            {
                Gtk.MenuItem renameItem = new Gtk.MenuItem(GettextCatalog.GetString("Rename"));
             
                renameItem.Activated += (sender, e) =>
                {
                    using (var dialog = new RenameDialog(itemToRename))
                    {
                        if (dialog.Run() == Command.Ok)
                        {
                            List<Failure> failures;

                            if (itemToRename.ItemType == ItemType.File)
                                TeamFoundationServerClient.Instance.PendRenameFile(_currentWorkspace, itemToRename.LocalItem, dialog.NewPath, out failures);
                            else
                                TeamFoundationServerClient.Instance.PendRenameFolder(_currentWorkspace, itemToRename.LocalItem, dialog.NewPath, out failures);

                            if (failures != null && failures.Any(f => f.SeverityType == SeverityType.Error))
                            {
                                foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
                                {
                                    MessageService.ShowError(failure.Message);
                                }
                            }      

                            TeamFoundationServerFileHelper.NotifyFilesChanged(_currentWorkspace, new List<ExtendedItem> { itemToRename });
                            Refresh(items);
                        }
                    }
                };

                editItems.Add(renameItem);
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

                            TeamFoundationServerClient.Instance.UndoChanges(_currentWorkspace, itemSpecs);

                            TeamFoundationServerFileHelper.NotifyFilesRemoved(_currentWorkspace, undoItems);
                            Refresh(items);
                        }
                    }
                };

                editItems.Add(undoItem);
            }

            // Delete
            var deleteItems = items.Where(i => !i.ChangeType.HasFlag(ChangeType.Delete)).ToList();

            if (deleteItems.Any())
            {
                Gtk.MenuItem deleteItem = new Gtk.MenuItem(GettextCatalog.GetString("Delete"));

                deleteItem.Activated += (sender, e) =>
                {
                    if (MessageService.Confirm(GettextCatalog.GetString("Are you sure you want to delete selected files?"), AlertButton.Yes))
                    {
                        List<Failure> failures;
                        _currentWorkspace.PendDelete(items.Select(x => (FilePath)x.LocalItem).ToList(), RecursionType.Full, false, out failures);

                        var errors = failures.Any(f => f.SeverityType == SeverityType.Error);

                        if (errors)
                        {
                            var failuresDialog = new FailuresDialog(failures);
                            failuresDialog.Show();
                        }

                        TeamFoundationServerFileHelper.NotifyFilesRemoved(_currentWorkspace, items);
                        Refresh(items);
                    }
                };
               
                editItems.Add(deleteItem);
            }

            return editItems;
        }

        List<Gtk.MenuItem> GetMapMenu(List<ExtendedItem> items)
        {
            Gtk.MenuItem mapItem = new Gtk.MenuItem(GettextCatalog.GetString("Map"));
            mapItem.Activated += (sender, e) => MapItem(items);

            return new List<Gtk.MenuItem> { mapItem };
        }

        #endregion

        #endregion
    }
}