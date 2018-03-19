// ChooseProjectsDialog.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
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

        internal List<ProjectCollection> SelectedProjectColletions { get; set; }

        internal ChooseProjectsDialog(TeamFoundationServer server)
        {
            Init();
            BuildGui(server);     
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

        void BuildGui(TeamFoundationServer server)
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

                if (isSelected) //Should add the project
                {
                    var collection = SelectedProjectColletions.SingleOrDefault(col => col == project.Collection);
                    if (collection == null)
                    {
                        collection = project.Collection.Copy();
                        collection.Projects.Add(project);
                        SelectedProjectColletions.Add(collection);
                    }
                    else
                    {
                        //Should not exists because now is selected
                        collection.Projects.Add(project);
                    }
                }
                else
                {
                    //Should exists because the project has been checked
                    var collection = SelectedProjectColletions.Single(pc => pc == project.Collection);
                    collection.Projects.Remove(project);
                    if (!collection.Projects.Any())
                    {
                        SelectedProjectColletions.Remove(collection);
                    }
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

            if (!server.ProjectCollections.Any())
                SelectedProjectColletions = new List<ProjectCollection>();
            else
                SelectedProjectColletions = new List<ProjectCollection>(server.ProjectCollections);
        }

        void LoadProjects(TeamFoundationServer server)
        {
            server.LoadStructure();

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
                    var selectedColletion = SelectedProjectColletions.SingleOrDefault(pc => pc == collection);

                    _projectsStore.Clear();

                    foreach (var project in collection.Projects)
                    {
                        var node = _projectsStore.AddNode();
                        var project1 = project;
                        var isSelected = selectedColletion != null && selectedColletion.Projects.Any(p => p == project1);
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