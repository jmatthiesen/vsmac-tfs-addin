using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Views
{
    public class ResolveConflictsView : ViewContent
    {
        TeamFoundationServerRepository _repository;
        List<FilePath> _paths;

        VBox _view;
        Button _acceptLocal;
        Button _merge;
        Button _acceptServer;
        Button _viewLocal;
        Button _viewServer;
        ListView _listView;
        DataField<Conflict> _itemField;
        DataField<string> _typeField;
        DataField<string> _nameField;
        DataField<int> _versionBaseField;
        DataField<int> _versionTheirField;
        DataField<int> _versionYourField;
        ListStore _listStore;

        public ResolveConflictsView()
        {
            Init();
            BuildGui();
        }

        public override Control Control => new XwtControl(_view);

        public static void Open(TeamFoundationServerRepository repository, List<FilePath> paths)
        {
            foreach (var view in IdeApp.Workbench.Documents)
            {
                var sourceDoc = view.GetContent<ResolveConflictsView>();
                if (sourceDoc != null)
                {
                    sourceDoc.GetData(repository, paths);
                    sourceDoc.LoadConflicts();
                    view.Window.SelectWindow();
                    return;
                }
            }

            ResolveConflictsView resolveConflictsView = new ResolveConflictsView();
            resolveConflictsView.GetData(repository, paths);
            resolveConflictsView.LoadConflicts();
            IdeApp.Workbench.OpenDocument(resolveConflictsView, true);
        }

        void Init()
        {
            _view = new VBox();
            _acceptLocal = new Button(GettextCatalog.GetString("Keep Local Version"));
            _merge = new Button(GettextCatalog.GetString("Merge"));
            _acceptServer = new Button(GettextCatalog.GetString("Take Server Version"));
            _viewLocal = new Button(GettextCatalog.GetString("View Local"));
            _viewServer = new Button(GettextCatalog.GetString("View Server"));
            _listView = new ListView();
            _itemField = new DataField<Conflict>();
            _typeField = new DataField<string>();
            _nameField = new DataField<string>();
            _versionBaseField = new DataField<int>();
            _versionTheirField = new DataField<int>();
            _versionYourField = new DataField<int>();
            _listStore = new ListStore(_typeField, _nameField, _itemField, _versionBaseField, _versionTheirField, _versionYourField);
            _paths = new List<FilePath>();
        }

        void BuildGui()
        {
            ContentName = GettextCatalog.GetString("Resolve Conflicts");
        
            HBox headerBox = new HBox();
            headerBox.PackStart(_acceptLocal);
            headerBox.PackStart(_merge);
            headerBox.PackStart(_acceptServer);
            headerBox.PackStart(_viewLocal);
            headerBox.PackStart(_viewServer);

            _acceptLocal.Clicked += (sender, e) => AcceptLocalClicked();
            _merge.Clicked += (sender, e) => AcceptMerge();
            _acceptServer.Clicked += (sender, e) => AcceptServerClicked();
            _viewLocal.Clicked += (sender, e) => ViewLocalClicked();
            _viewServer.Clicked += (sender, e) => ViewRemoteClicked();

            _view.PackStart(headerBox);

            _listView.Columns.Add("Conflict Type", _typeField);
            _listView.Columns.Add("Item Name", _nameField);
            _listView.Columns.Add("Base Version", _versionBaseField);
            _listView.Columns.Add("Server Version", _versionTheirField);
            _listView.Columns.Add("Your Version", _versionYourField);
          
            _listView.RowActivated += (sender, e) => RowClicked();
            _listView.SelectionChanged += (sender, e) => SetButtonSensitive();

            _listView.DataSource = _listStore;
            _view.PackStart(_listView, true, true);
        }

        void GetData(TeamFoundationServerRepository repository, List<FilePath> paths)
        {
            _repository = repository;
            _paths.Clear();
            _paths.AddRange(paths);
        }

        async void RowClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            var doc = await IdeApp.Workbench.OpenDocument(conflict.TargetLocalItem, null, true);

            if (doc != null)
            {
                doc.Window.SwitchView(doc.Window.FindView<VersionControl.Views.IDiffView>());
            }
        }

        void SetButtonSensitive()
        {
            _acceptLocal.Sensitive = _acceptServer.Sensitive = (_listView.SelectedRow > -1);
        }

        void AcceptLocalClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            _repository.Resolve(conflict, ResolutionType.AcceptYours);
            LoadConflicts();
        }

        void AcceptMerge()
        {
            var mergeToolInfo = TeamFoundationServerClient.Settings.MergeTool;

            if (mergeToolInfo != null)
            {
                var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
                var downloadService = _repository.VersionControlService.Collection.GetService<VersionControlDownloadService>();

                var baseFile = downloadService.DownloadToTemp(conflict.BaseDowloadUrl);
                var theirsFile = downloadService.DownloadToTemp(conflict.TheirDowloadUrl);
              
                var arguments = mergeToolInfo.Arguments;
          
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = mergeToolInfo.CommandName;
                info.Arguments = arguments;
                var process = Process.Start(info);
                process.WaitForExit();

                FileHelper.FileMove(baseFile, conflict.TargetLocalItem, true);
                FileHelper.FileDelete(theirsFile);

                conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
                _repository.Resolve(conflict, ResolutionType.AcceptMerge);
                LoadConflicts();
            }
        }

        void AcceptServerClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            _repository.Resolve(conflict, ResolutionType.AcceptTheirs);
            LoadConflicts();
        }

        async void ViewLocalClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            var downloadService = _repository.VersionControlService.Collection.GetService<VersionControlDownloadService>();
            var fileName = downloadService.DownloadToTemp(conflict.BaseDowloadUrl);
           
            var doc = await IdeApp.Workbench.OpenDocument(fileName, null, true);
            doc.Window.ViewContent.ContentName = Path.GetFileName(conflict.TargetLocalItem) + " - v" + conflict.BaseVersion;
           
            doc.Closed += (o, e) => FileHelper.FileDelete(fileName);
        }

        async void ViewRemoteClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            var downloadService = _repository.VersionControlService.Collection.GetService<VersionControlDownloadService>();
            var fileName = downloadService.DownloadToTemp(conflict.TheirDowloadUrl);
           
            var doc = await IdeApp.Workbench.OpenDocument(fileName, null, true);
            doc.Window.ViewContent.ContentName = Path.GetFileName(conflict.TargetLocalItem) + " - v" + conflict.TheirVersion;
           
            doc.Closed += (o, e) => FileHelper.FileDelete(fileName);
        }

        void LoadConflicts()
        {
            if (_paths.Count == 0)
                return;
            
            var conflicts = _repository.GetConflicts(_paths);

            _listStore.Clear();
            foreach (var conflict in conflicts)
            {
                var row = _listStore.AddRow();
                _listStore.SetValue(row, _itemField, conflict);
                _listStore.SetValue(row, _typeField, conflict.ConflictType.ToString());
              
                var path = ((FilePath)conflict.TargetLocalItem).ToRelative(_paths[0]);
              
                _listStore.SetValue(row, _nameField, path);
                _listStore.SetValue(row, _versionBaseField, conflict.BaseVersion);
                _listStore.SetValue(row, _versionTheirField, conflict.TheirVersion);
                _listStore.SetValue(row, _versionYourField, conflict.YourVersion);
            }
        }
    }
}