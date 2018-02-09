using System;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using MonoDevelop.Core;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs
{
    public class AddWorkspaceDialog : Dialog
    {
        ListView _foldersView;
        DataField<string> _tfsFolder;
        DataField<string> _localFolder;
        ListStore _foldersStore;
        TextEntry _nameEntry;
        TextEntry _ownerEntry;
        TextEntry _computerEntry;
        ProjectCollection _projectCollection;

        public AddWorkspaceDialog(ProjectCollection projectCollection)
        {
            Init(projectCollection);
            BuildGui();
            FillDefaultData();
        }

        void Init(ProjectCollection projectCollection)
        {
            _projectCollection = projectCollection;
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
            Title = "Add Workspace" + " - " + _projectCollection.Server.Name + " - " + _projectCollection.Name;

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

            Button addButton = new Button(GettextCatalog.GetString("Add Working Folder"));
            addButton.MinWidth = GuiSettings.ButtonWidth;
            addButton.Clicked += OnAddWorkingFolder;

            Button okButton = new Button(GettextCatalog.GetString("OK"));
            okButton.MinWidth = GuiSettings.ButtonWidth;
            okButton.Clicked += OnAddWorkspace;

            Button cancelButton = new Button(GettextCatalog.GetString("Cancel"));
            cancelButton.MinWidth = GuiSettings.ButtonWidth;
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
            WorkspaceData workspaceData = new WorkspaceData();
            workspaceData.Name = _nameEntry.Text;
            workspaceData.Owner = _ownerEntry.Text;
            workspaceData.Computer = _computerEntry.Text;

            for (int i = 0; i < _foldersStore.RowCount; i++)
            {
                var tfsFolder = _foldersStore.GetValue(i, _tfsFolder);
                var localFolder = _foldersStore.GetValue(i, _localFolder);
                workspaceData.WorkingFolders.Add(new WorkingFolder(tfsFolder, localFolder));
            }

            var versionControl = _projectCollection.GetService<RepositoryService>();
            versionControl.CreateWorkspace(new Workspace(versionControl, workspaceData));  
        }

        void FillDefaultData()
        {
            _nameEntry.Text = _computerEntry.Text = Environment.MachineName;
            _ownerEntry.Text = _projectCollection.Server.UserName;
        }
    }
}