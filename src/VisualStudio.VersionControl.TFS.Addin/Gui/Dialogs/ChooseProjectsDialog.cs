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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
	/// <summary>
    /// Choose projects dialog.
    /// </summary>
    public class ChooseProjectsDialog : Dialog
    {
        ListStore _collectionStore;
        ListBox _collectionsList;
        TreeStore _projectsStore;
        TreeView _projectsList;
        DataField<ProjectCollection> _collectionItem;
        DataField<string> _collectionName;
        DataField<bool> _isProjectSelected;
        DataField<Xwt.Drawing.Image> _projectType;
        DataField<string> _projectName;
        DataField<ProjectInfo> _projectItem;
        CheckBoxCellView _checkView;
		Spinner _projectsSpinner;

		Task _worker;
        CancellationTokenSource _workerCancel;

        internal List<ProjectCollection> SelectedProjectColletions { get; set; }

        internal ChooseProjectsDialog(TeamFoundationServer server)
        {
            Init();
            BuildGui(server);
            LoadProjects(server);
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
		/// Init ChooseProjectsDialog.
        /// </summary>
        void Init()
        {
			_workerCancel = new CancellationTokenSource();

            _collectionsList = new ListBox();
            _projectsList = new TreeView();

            _collectionItem = new DataField<ProjectCollection>();
            _collectionName = new DataField<string>();
            _isProjectSelected = new DataField<bool>();
            _projectType = new DataField<Xwt.Drawing.Image>();
            _projectName = new DataField<string>();
            _projectItem = new DataField<ProjectInfo>();

            _collectionStore = new ListStore(_collectionName, _collectionItem);
            _projectsStore = new TreeStore(_isProjectSelected, _projectType, _projectName, _projectItem);

			_projectsSpinner = new Spinner
            {
                HeightRequest = 24,
                WidthRequest = 24,
                Animate = true,
                HorizontalPlacement = WidgetPlacement.Center,
                VerticalPlacement = WidgetPlacement.Center
            };
        }

        /// <summary>
		/// Builds the ChooseProjectsDialog GUI.
        /// </summary>
        /// <param name="server">Server.</param>
        void BuildGui(TeamFoundationServer server)
        {
			Title = GettextCatalog.GetString("Choose Projects");
            
			var vBox = new VBox
			{
				HeightRequest = 350,
				WidthRequest = 500
			};

			vBox.PackStart(_projectsSpinner, true, WidgetPlacement.Center);

            var hbox = new HBox();

            _collectionsList.DataSource = _collectionStore;
            _collectionsList.Views.Add(new TextCellView(_collectionName));
			_collectionsList.MinHeight = 300;
			_collectionsList.MinWidth = 200;
            hbox.PackStart(_collectionsList);

            _projectsList.DataSource = _projectsStore;
			_projectsList.MinHeight = 300;
			_projectsList.MinWidth = 300;

            _checkView = new CheckBoxCellView(_isProjectSelected) { Editable = true };

            _checkView.Toggled += (sender, e) =>
            {
                var row = _projectsList.CurrentEventRow;
                var node = _projectsStore.GetNavigatorAt(row);
                var isSelected = !node.GetValue(_isProjectSelected); // Xwt gives previous value
                var project = node.GetValue(_projectItem);

                if (isSelected) // Should add the project
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
                        // Should not exists because now is selected
                        collection.Projects.Add(project);
                    }
                }
                else
                {
                    // Should exists because the project has been checked
                    var collection = SelectedProjectColletions.Single(pc => pc == project.Collection);
                    collection.Projects.Remove(project);
                 
                    if (!collection.Projects.Any())
                    {
                        SelectedProjectColletions.Remove(collection);
                    }
                }
            };

            _projectsList.Columns.Add(new ListViewColumn("", _checkView));
            var projectTypeColumn = new ListViewColumn("Type");
            projectTypeColumn.Views.Add(new ImageCellView(_projectType));
            _projectsList.Columns.Add(projectTypeColumn);
            _projectsList.Columns.Add(new ListViewColumn("Name", new TextCellView(_projectName)));
            hbox.PackEnd(_projectsList);

            vBox.PackStart(hbox);

            Button okButton = new Button(GettextCatalog.GetString("OK"));
            okButton.Clicked += (sender, e) => Respond(Command.Ok);

            Button cancelButton = new Button(GettextCatalog.GetString("Cancel"));
            cancelButton.Clicked += (sender, e) => Respond(Command.Cancel);

            okButton.MinWidth = cancelButton.MinWidth = GuiSettings.ButtonWidth;

            var buttonBox = new HBox();
            buttonBox.PackEnd(okButton);
            buttonBox.PackEnd(cancelButton);
			vBox.PackEnd(buttonBox);

            Content = vBox;
			Resizable = false;

            if (!server.ProjectCollections.Any())
                SelectedProjectColletions = new List<ProjectCollection>();
            else
                SelectedProjectColletions = new List<ProjectCollection>(server.ProjectCollections);
        }

        /// <summary>
        /// Loading. Show or hide some UI elements.
        /// </summary>
        /// <param name="isLoading">If set to <c>true</c> is loading.</param>
        void Loading(bool isLoading)
		{
			_projectsSpinner.Visible = isLoading;
			_collectionsList.Visible = !isLoading;
			_projectsList.Visible = !isLoading;
		}
   
        /// <summary>
        /// Loads the projects.
        /// </summary>
		/// <param name="server">TeamFoundationServer.</param>
        void LoadProjects(TeamFoundationServer server)
        {
			Loading(true);

			_worker = Task.Factory.StartNew(delegate
			{
				if (!_workerCancel.Token.IsCancellationRequested)
				{
					server.LoadStructure();

					Application.Invoke(() =>
					{
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
								var selectedColletion = SelectedProjectColletions.FirstOrDefault(pc => pc == collection);

								_projectsStore.Clear();

								foreach (var project in collection.Projects)
								{
									var node = _projectsStore.AddNode();
									var projectCopy = project;

									var isSelected = selectedColletion != null && selectedColletion.Projects.Any(p => p == projectCopy);
									node.SetValue(_isProjectSelected, isSelected);
									node.SetValue(_projectType, GetProjectTypeImage(project.ProjectDetails));
									node.SetValue(_projectName, project.Name);
									node.SetValue(_projectItem, project);
								}
							}
						};

						if (server.ProjectCollections.Any())
							_collectionsList.SelectRow(0);

						Loading(false);
					});               
				}
			}, _workerCancel.Token, TaskCreationOptions.LongRunning);
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

        Xwt.Drawing.Image GetProjectTypeImage(ProjectDetails projectDetails)
        {
            if(IsSourceControlGitEnabled(projectDetails))
            {
               return Xwt.Drawing.Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.Git.png").WithSize(16, 16);
            }

            return Xwt.Drawing.Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.VSTS.png").WithSize(16, 16);
        }
    }
}