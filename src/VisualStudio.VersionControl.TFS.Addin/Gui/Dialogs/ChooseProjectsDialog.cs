using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class ChooseProjectsDialog : Dialog
    {
        ListStore _collectionStore;
        ListBox _collectionsList;
        TreeStore _projectsStore;
        TreeView _projectsList;
        DataField<ProjectCollection> _collectionItem;
        DataField<string> _collectionName;
        DataField<bool> _isProjectSelected;
        DataField<string> _projectName;
        DataField<ProjectInfo> _projectItem;

        public List<ProjectInfo> SelectedProjects { get; set; }

        public ChooseProjectsDialog(BaseTeamFoundationServer server)
        {
            Init();
            BuildGui();     
            LoadProjects(server);
        }

        void Init()
        {
            _collectionsList = new ListBox();
            _projectsList = new TreeView();

            _collectionItem = new DataField<ProjectCollection>();
            _collectionName = new DataField<string>();
            _isProjectSelected = new DataField<bool>();
            _projectName = new DataField<string>();
            _projectItem = new DataField<ProjectInfo>();

            _collectionStore = new ListStore(_collectionName, _collectionItem);
            _projectsStore = new TreeStore(_isProjectSelected, _projectName, _projectItem);
        }

        void BuildGui()
        {
            Title = "Select Projects";
            Resizable = false;

            var vBox = new VBox();
            var hbox = new HBox();
            _collectionsList.DataSource = _collectionStore;
            _collectionsList.Views.Add(new TextCellView(_collectionName));
            _collectionsList.MinWidth = 200;
            _collectionsList.MinHeight = 300;
            hbox.PackStart(_collectionsList);

            _projectsList.DataSource = _projectsStore;
            _projectsList.MinWidth = 200;
            _projectsList.MinHeight = 300;

            var checkView = new CheckBoxCellView(_isProjectSelected) { Editable = true };

            checkView.Toggled += (sender, e) =>
            {
                var row = _projectsList.CurrentEventRow;
                var node = _projectsStore.GetNavigatorAt(row);
                var isSelected = !node.GetValue(_isProjectSelected); //Xwt gives previous value
                var project = node.GetValue(_projectItem);

                if (isSelected && !SelectedProjects.Any(p => string.Equals(p.Uri, project.Uri)))
                {
                    SelectedProjects.Add(project);
                }

                if (!isSelected && SelectedProjects.Any(p => string.Equals(p.Uri, project.Uri)))
                {
                    SelectedProjects.RemoveAll(p => string.Equals(p.Uri, project.Uri));
                }
            };

            _projectsList.Columns.Add(new ListViewColumn("", checkView));
            _projectsList.Columns.Add(new ListViewColumn("Name", new TextCellView(_projectName)));
            hbox.PackEnd(_projectsList);

            vBox.PackStart(hbox);

            Button ok = new Button(GettextCatalog.GetString("OK"));
            ok.Clicked += (sender, e) => Respond(Command.Ok);

            Button cancel = new Button(GettextCatalog.GetString("Cancel"));
            cancel.Clicked += (sender, e) => Respond(Command.Cancel);

            ok.MinWidth = cancel.MinWidth = GuiSettings.ButtonWidth;

            var buttonBox = new HBox();
            buttonBox.PackEnd(ok);
            buttonBox.PackEnd(cancel);
            vBox.PackStart(buttonBox);

            Content = vBox;
        }

        void LoadProjects(BaseTeamFoundationServer server)
        {
            TeamFoundationServerClient.Instance.LoadProjects(server);

            if (server.ProjectCollections == null)
                SelectedProjects = new List<ProjectInfo>();
            else
                SelectedProjects = new List<ProjectInfo>(server.ProjectCollections.SelectMany(pc => pc.Projects));  
            
            foreach (var col in server.ProjectCollections)
            {
                var row = _collectionStore.AddRow();
                _collectionStore.SetValue(row, _collectionName, col.Name);
                _collectionStore.SetValue(row, _collectionItem, col);
            }

            _collectionsList.SelectionChanged += (sender, e) =>
            {
                if (_collectionsList.SelectedRow > -1)
                {
                    var collection = _collectionStore.GetValue(_collectionsList.SelectedRow, _collectionItem);
                    _projectsStore.Clear();

                    foreach (var project in collection.Projects)
                    {
                        var node = _projectsStore.AddNode();
                        var project1 = project;
                        var isSelected = SelectedProjects.Any(x => string.Equals(x.Uri, project1.Uri, StringComparison.OrdinalIgnoreCase));
                        node.SetValue(_isProjectSelected, isSelected);
                        node.SetValue(_projectName, project.Name);    
                        node.SetValue(_projectItem, project);
                    }
                }
            };

            if (server.ProjectCollections.Any())
                _collectionsList.SelectRow(0);
        }
    }
}