using System;
using System.Collections.Generic;
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
        IWorkspace _currentWorkspace;

        Gtk.VBox _view;
        Gtk.Button _manageButton;
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

            _workspaceComboBox = new Gtk.ComboBox();
            _workspaceStore = new Gtk.ListStore(typeof(Workspace), typeof(string));

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

        void FireFilesChanged(List<ExtendedItem> items)
        {
            FileService.NotifyFilesChanged(items.Select(i => new FilePath(_currentWorkspace.Data.GetLocalPathForServerPath(i.ServerPath))), true);
        }

        void FireFilesRemoved(List<ExtendedItem> items)
        {
            FileService.NotifyFilesRemoved(items.Select(i => new FilePath(_currentWorkspace.Data.GetLocalPathForServerPath(i.ServerPath))));
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
                    _currentWorkspace.Map(item.ServerPath, folderSelect.Folder);
                }

                Refresh(items);
            }
        }

        void GetWorkspaces()
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

        void GetData()
        {
            _treeStore.Clear();

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

                    FireFilesChanged(checkOutItems);
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
                        _currentWorkspace.LockItems(itemsToLock.Select(i => i.ServerPath), lockLevel);               
                        progress.EndTask();
                        progress.ReportSuccess("Finish locking.");
                    }

                    FireFilesChanged(lockItems);
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
                    _currentWorkspace.UnLockItems(unLockItems.Select(i => i.ServerPath));
                    FireFilesChanged(unLockItems);
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
                            ICollection<Failure> failures;

                            _currentWorkspace.PendRename(itemToRename.LocalPath, dialog.NewPath, out failures);
                      
                            if (failures != null && failures.Any(f => f.SeverityType == SeverityType.Error))
                            {
                                foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
                                {
                                    MessageService.ShowError(failure.Message);
                                }
                            }      

                            FireFilesChanged(new List<ExtendedItem> { itemToRename });
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

                            _currentWorkspace.Undo(itemSpecs);
                          
                            FireFilesChanged(undoItems);
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