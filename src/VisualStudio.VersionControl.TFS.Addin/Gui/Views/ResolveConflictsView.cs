﻿using System.Collections.Generic;
using System.IO;
using Autofac;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Views
{
    public class ResolveConflictsView : ViewContent
    {
        List<LocalPath> _paths;
        IWorkspace _workspace;
        TeamFoundationServerVersionControlService _versionControlService;

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

        internal static void Open(IWorkspace workspace, List<LocalPath> paths)
        {
            foreach (var view in IdeApp.Workbench.Documents)
            {
                var sourceDoc = view.GetContent<ResolveConflictsView>();
                if (sourceDoc != null)
                {
                    sourceDoc.GetData(workspace, paths);
                    sourceDoc.LoadConflicts();
                    view.Window.SelectWindow();
                    return;
                }
            }

            ResolveConflictsView resolveConflictsView = new ResolveConflictsView();
            resolveConflictsView.GetData(workspace, paths);
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
            _paths = new List<LocalPath>();
         
            _versionControlService = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();
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

        void GetData(IWorkspace workspace, List<LocalPath> paths)
        {
            _workspace = workspace;
            _paths.Clear();
            _paths.AddRange(paths);
        }

        async void RowClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            var doc = await IdeApp.Workbench.OpenDocument(new FilePath(conflict.TargetLocalItem), null, true);

            if (doc != null)
            {
                doc.Window.SwitchView(doc.Window.FindView<MonoDevelop.VersionControl.Views.IDiffView>());
            }
        }

        void SetButtonSensitive()
        {
            _acceptLocal.Sensitive = _acceptServer.Sensitive = (_listView.SelectedRow > -1);
        }

        void AcceptLocalClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            _workspace.Resolve(conflict, ResolutionType.AcceptYours);
            LoadConflicts();
        }

        void AcceptMerge()
        {
            // TODO:
        }

        void AcceptServerClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            _workspace.Resolve(conflict, ResolutionType.AcceptTheirs);
            LoadConflicts();
        }

        async void ViewLocalClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            var fileName = _workspace.DownloadToTemp(conflict.BaseDowloadUrl);
           
            var doc = await IdeApp.Workbench.OpenDocument(new FilePath(fileName), null, true);
            doc.Window.ViewContent.ContentName = Path.GetFileName(conflict.TargetLocalItem) + " - v" + conflict.BaseVersion;
           
            doc.Closed += (o, e) => fileName.Delete();
        }

        async void ViewRemoteClicked()
        {
            var conflict = _listStore.GetValue(_listView.SelectedRow, _itemField);
            var fileName = _workspace.DownloadToTemp(conflict.TheirDowloadUrl);
           
            var doc = await IdeApp.Workbench.OpenDocument(new FilePath(fileName), null, true);
            doc.Window.ViewContent.ContentName = Path.GetFileName(conflict.TargetLocalItem) + " - v" + conflict.TheirVersion;
           
            doc.Closed += (o, e) => fileName.Delete();
        }

        void LoadConflicts()
        {
            if (_paths.Count == 0)
                return;
            
            var conflicts = _workspace.GetConflicts(_paths);

            _listStore.Clear();
            foreach (var conflict in conflicts)
            {
                var row = _listStore.AddRow();
                _listStore.SetValue(row, _itemField, conflict);
                _listStore.SetValue(row, _typeField, conflict.ConflictType.ToString());
              
                var path = conflict.TargetLocalItem.ToRelativeOf(_paths[0]);
              
                _listStore.SetValue(row, _nameField, path);
                _listStore.SetValue(row, _versionBaseField, conflict.BaseVersion);
                _listStore.SetValue(row, _versionTheirField, conflict.TheirVersion);
                _listStore.SetValue(row, _versionYourField, conflict.YourVersion);
            }
        }
    }
}