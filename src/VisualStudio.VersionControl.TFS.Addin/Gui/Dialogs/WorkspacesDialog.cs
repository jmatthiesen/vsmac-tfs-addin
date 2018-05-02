// WorkspacesDialog.cs
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
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
	/// <summary>
    /// Workspaces dialog.
    /// </summary>
    public class WorkspacesDialog : Dialog
    {
        ProjectCollection _projectCollection;
        WorkspaceData _workspaceData;
        ListView _listView;
        ListStore _listStore;
        DataField<string> _name;
        DataField<string> _computer;
        DataField<string> _owner;
        Button _editWorkspaceButton;
        CheckBox _showRemoteCheck;

        internal WorkspacesDialog(ProjectCollection projectCollection)
        {
            _projectCollection = projectCollection;

            Title = GettextCatalog.GetString("Manage Workspaces");
           
            Init();
            BuildGui();
			LoadWorkspaces();
        }

        /// <summary>
		/// Init WorkspacesDialog.
        /// </summary>
        void Init()
        {
			_listView = new ListView
			{
				MinHeight = 150,
				MinWidth = 600
			};

			_name = new DataField<string>();
            _computer = new DataField<string>();
            _owner = new DataField<string>();

            _listStore = new ListStore(_name, _computer, _owner);

            _listView.Columns.Add(new ListViewColumn(GettextCatalog.GetString("Name"), new TextCellView(_name)));
            _listView.Columns.Add(new ListViewColumn(GettextCatalog.GetString("Computer"), new TextCellView(_computer)));
            _listView.Columns.Add(new ListViewColumn(GettextCatalog.GetString("Owner"), new TextCellView(_owner)));

            _listView.DataSource = _listStore;   

            _showRemoteCheck = new CheckBox();
        }

        /// <summary>
		/// Builds the WorkspacesDialog GUI.
        /// </summary>
        void BuildGui()
        {
            VBox content = new VBox();

            content.PackStart(new Label(GettextCatalog.GetString("Workspaces:")));
            _listView.SelectionChanged += (sender, args) => UpdateEditWorkspace();
            content.PackStart(_listView);

            HBox remoteBox = new HBox();

			_showRemoteCheck.Clicked += (sender, e) => LoadWorkspaces();
            remoteBox.PackStart(_showRemoteCheck);
            remoteBox.PackStart(new Label(GettextCatalog.GetString("Show remote workspaces")));
            content.PackStart(remoteBox);

            HBox buttonBox = new HBox();

            Button addWorkspaceButton = new Button(GettextCatalog.GetString("Add")) { MinWidth = GuiSettings.ButtonWidth };
            addWorkspaceButton.Clicked += AddWorkspaceClick;

            _editWorkspaceButton = new Button(GettextCatalog.GetString("Edit")) { MinWidth = GuiSettings.ButtonWidth };
            _editWorkspaceButton.Clicked += EditWorkspaceClick;

            Button removeWorkspaceButton = new Button(GettextCatalog.GetString("Remove")) { MinWidth = GuiSettings.ButtonWidth };
            removeWorkspaceButton.Clicked += RemoveWorkspaceClick;

            Button closeButton = new Button(GettextCatalog.GetString("Close")) { MinWidth = GuiSettings.ButtonWidth };
            closeButton.Clicked += (sender, e) => Respond(Command.Close);

            buttonBox.PackStart(addWorkspaceButton);
            buttonBox.PackStart(_editWorkspaceButton);
            buttonBox.PackStart(removeWorkspaceButton);
            buttonBox.PackEnd(closeButton);

            content.PackStart(buttonBox);

            Content = content;
            Resizable = false;
        }

        /// <summary>
        /// Loads the workspaces.
        /// </summary>
        void LoadWorkspaces()
        {
            _listStore.Clear();

            try
            {
                var remotes = _showRemoteCheck.State == CheckBoxState.On;
                var workspaces = remotes ? _projectCollection.GetRemoteWorkspaces() : _projectCollection.GetLocalWorkspaces();

                foreach (var workspace in workspaces)
                {
                    var row = _listStore.AddRow();

                    _listStore.SetValue(row, _name, workspace.Name);
                    _listStore.SetValue(row, _computer, workspace.Computer);
                    _listStore.SetValue(row, _owner, workspace.Owner);
                }

                UpdateEditWorkspace();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Adds workspace click event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        void AddWorkspaceClick(object sender, EventArgs e)
        {
            using (var dialog = new AddEditWorkspaceDialog(_projectCollection, null))
            {
                if (dialog.Run(this) == Command.Ok)
                {
					LoadWorkspaces();
                }
            }
        }

        /// <summary>
        /// Edits the workspace click event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        void EditWorkspaceClick(object sender, EventArgs e)
        {
            string workspaceName = _listStore.GetValue(_listView.SelectedRow, _name);
            _workspaceData = _projectCollection.GetWorkspace(workspaceName);

            using (var dialog = new AddEditWorkspaceDialog(_projectCollection, _workspaceData))
            {
                if (dialog.Run(this) == Command.Ok)
                {
					LoadWorkspaces();
                }
            }
        }

        /// <summary>
        /// Removes the workspace click event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        void RemoveWorkspaceClick(object sender, EventArgs e)
        {
            if (_listView.SelectedRow > -1 &&
                MessageService.Confirm(GettextCatalog.GetString("Are you sure you want to delete selected workspace?"), AlertButton.Yes))
            {
                var name = _listStore.GetValue(_listView.SelectedRow, _name);
                var owner = _listStore.GetValue(_listView.SelectedRow, _owner);
                _projectCollection.DeleteWorkspace(name, owner);

				LoadWorkspaces();
            }       
        }

        /// <summary>
        /// Updates the edit workspace button state.
        /// </summary>
        void UpdateEditWorkspace()
        {
            _editWorkspaceButton.Sensitive = _listView.SelectedRow != -1;
        }
    }
}