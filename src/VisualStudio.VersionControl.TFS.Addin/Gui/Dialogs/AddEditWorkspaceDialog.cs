// AddEditWorkspaceDialog.cs
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
    public class AddEditWorkspaceDialog : Dialog
    {
        ListView _foldersView;
        DataField<string> _tfsFolder;
        DataField<string> _localFolder;
        ListStore _foldersStore;
        TextEntry _nameEntry;
        TextEntry _ownerEntry;
        TextEntry _computerEntry;
        ProjectCollection _projectCollection;
        WorkspaceData _workspaceData;

        internal AddEditWorkspaceDialog(ProjectCollection projectCollection, WorkspaceData workspaceData)
        {
            Init(projectCollection, workspaceData);
            BuildGui();

            if (_workspaceData != null)
            {
                FillData();
                FillWorkingFolders();
            }
            else
            {
                FillDefaultData();
            }
        }

        void Init(ProjectCollection projectCollection, WorkspaceData workspaceData)
        {
            _projectCollection = projectCollection;
            _workspaceData = workspaceData;
            _foldersView = new ListView();    
            _tfsFolder = new DataField<string>();
            _localFolder = new DataField<string>();
            _foldersStore = new ListStore(_tfsFolder, _localFolder);
            _nameEntry = new TextEntry();
            _ownerEntry = new TextEntry();   
            _computerEntry = new TextEntry();
        }

        void BuildGui()
        {
            if (_workspaceData != null)
            {
                Title = "Edit Workspace " + _workspaceData.Name + " - " + _projectCollection.Server.Name + " - " + _projectCollection.Name;
            }
            else
            {
                Title = "Create new Workspace" + " - " + _projectCollection.Server.Name + " - " + _projectCollection.Name;
            }

            VBox content = new VBox();

            Table entryTable = new Table();
            entryTable.Add(new Label(GettextCatalog.GetString("Name") + ":"), 0, 0);
            entryTable.Add(_nameEntry, 1, 0);

            entryTable.Add(new Label(GettextCatalog.GetString("Owner") + ":"), 0, 1);
            entryTable.Add(_ownerEntry, 1, 1);

            entryTable.Add(new Label(GettextCatalog.GetString("Computer") + ":"), 0, 2);
            entryTable.Add(_computerEntry, 1, 2);

            content.PackStart(entryTable);

            content.PackStart(new Label(GettextCatalog.GetString("Working folders") + ":"));
            _foldersView.DataSource = _foldersStore;
            _foldersView.MinHeight = 150;

            var tfsFolderView = new TextCellView(_tfsFolder);
            tfsFolderView.Editable = true;

            var localFolderView = new TextCellView(_localFolder);

            _foldersView.Columns.Add(new ListViewColumn("Source Control", tfsFolderView));
            _foldersView.Columns.Add(new ListViewColumn("Local", localFolderView));

            content.PackStart(_foldersView);

            HBox buttonBox = new HBox();

            Button addButton = new Button(GettextCatalog.GetString("Add Working Folder"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };
            addButton.Clicked += OnAddWorkingFolder;

            Button okButton = new Button(GettextCatalog.GetString("OK"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };
            okButton.Clicked += OnAddWorkspace;

            Button cancelButton = new Button(GettextCatalog.GetString("Cancel"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };
            cancelButton.Clicked += (sender, e) => Respond(Command.Cancel);

            buttonBox.PackStart(addButton);
            buttonBox.PackStart(okButton);
            buttonBox.PackEnd(cancelButton);

            content.PackStart(buttonBox);

            Content = content;
            Resizable = false;
        }

        void OnAddWorkingFolder(object sender, EventArgs e)
        {
            using (var projectSelect = new SelectProjectDialog(_projectCollection))
            {
                if (projectSelect.Run(this) == Command.Ok && !string.IsNullOrEmpty(projectSelect.SelectedPath))
                {
                    using (SelectFolderDialog folderSelect = new SelectFolderDialog("Browse For Folder"))
                    {
                        folderSelect.Multiselect = false;
                        folderSelect.CanCreateFolders = true;

                        if (folderSelect.Run(this))
                        {
                            var row = _foldersStore.AddRow();
                            _foldersStore.SetValue(row, _tfsFolder, projectSelect.SelectedPath);
                            _foldersStore.SetValue(row, _localFolder, folderSelect.Folder);
                        }
                    }
                }
            }
        }

        void OnAddWorkspace(object sender, EventArgs e)
        {
            try
            {
                WorkspaceData workspaceData = new WorkspaceData
                {
                    Name = _nameEntry.Text,
                    Owner = _ownerEntry.Text,
                    Computer = _computerEntry.Text
                };

                for (int i = 0; i < _foldersStore.RowCount; i++)
                {
                    var tfsFolder = _foldersStore.GetValue(i, _tfsFolder);
                    var localFolder = _foldersStore.GetValue(i, _localFolder);
                    workspaceData.WorkingFolders.Add(new WorkingFolder(tfsFolder, localFolder));
                }

                if (_workspaceData == null)
                {
                    _projectCollection.CreateWorkspace(workspaceData);
                }
                else
                {
                    _projectCollection.UpdateWorkspace(_workspaceData.Name, _workspaceData.Owner, workspaceData);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageService.ShowError(GettextCatalog.GetString("Cannot create the workspace. Please, try again."));
            }

            Respond(Command.Ok);
        }

        void FillDefaultData()
        {
            _nameEntry.Text = _computerEntry.Text = Environment.MachineName;
            _ownerEntry.Text = _projectCollection.Server.UserName;
        }

        void FillData()
        {
            _nameEntry.Text = _workspaceData.Name;
            _ownerEntry.Text = _workspaceData.Owner;
            _computerEntry.Text = _workspaceData.Computer;
        }

        void FillWorkingFolders()
        {
            foreach (var workingFolder in _workspaceData.WorkingFolders)
            {
                var row = _foldersStore.AddRow();
                _foldersStore.SetValue(row, _tfsFolder, workingFolder.ServerItem);
                _foldersStore.SetValue(row, _localFolder, workingFolder.LocalItem);
            }
        }
    }
}