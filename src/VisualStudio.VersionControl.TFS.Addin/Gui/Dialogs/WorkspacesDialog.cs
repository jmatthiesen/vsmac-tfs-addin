using System;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class WorkspacesDialog : Dialog
    {
        ProjectCollection _projectCollection;
        ListView _listView;
        ListStore _listStore;
        DataField<string> _name;
        DataField<string> _computer;
        DataField<string> _owner;

        internal WorkspacesDialog(ProjectCollection projectCollection)
        {
            _projectCollection = projectCollection;

            Title = GettextCatalog.GetString("Manage Workspaces");
           
            Init();
            BuildGui();
            GetWorkspaces();
        }

        void Init()
        {
            _listView = new ListView();

            _name = new DataField<string>();
            _computer = new DataField<string>();
            _owner = new DataField<string>();

            _listStore = new ListStore(_name, _computer, _owner);

            _listView.Columns.Add(new ListViewColumn(GettextCatalog.GetString("Name"), new TextCellView(_name)));
            _listView.Columns.Add(new ListViewColumn(GettextCatalog.GetString("Computer"), new TextCellView(_computer)));
            _listView.Columns.Add(new ListViewColumn(GettextCatalog.GetString("Owner"), new TextCellView(_owner)));

            _listView.DataSource = _listStore;   
        }

        void BuildGui()
        {
            VBox content = new VBox();

            content.PackStart(new Label(GettextCatalog.GetString("Workspaces:")));
            content.PackStart(_listView);

            HBox buttonBox = new HBox();
            Button addWorkspaceButton = new Button(GettextCatalog.GetString("Add")) { MinWidth = GuiSettings.ButtonWidth };
            addWorkspaceButton.Clicked += AddWorkspaceClick;

            Button removeWorkspaceButton = new Button(GettextCatalog.GetString("Remove")) { MinWidth = GuiSettings.ButtonWidth };
            removeWorkspaceButton.Clicked += RemoveWorkspaceClick;

            Button closeButton = new Button(GettextCatalog.GetString("Close")) { MinWidth = GuiSettings.ButtonWidth };
            closeButton.Clicked += (sender, e) => Respond(Command.Close);

            buttonBox.PackStart(addWorkspaceButton);
            buttonBox.PackStart(removeWorkspaceButton);
            buttonBox.PackEnd(closeButton);

            content.PackStart(buttonBox);

            Content = content;
            Resizable = false;
        }

        void GetWorkspaces()
        {
            /*
            var workspaces = TeamFoundationServerClient.Instance.GetWorkspaces(_projectCollection);
            _listStore.Clear();

            foreach (var workspace in workspaces)
            {
                var row = _listStore.AddRow();
                _listStore.SetValue(row, _name, workspace.Name);
                _listStore.SetValue(row, _computer, workspace.Computer);
                _listStore.SetValue(row, _owner, workspace.OwnerName);
            }
            */
        }

        void AddWorkspaceClick(object sender, EventArgs e)
        {
            using (var dialog = new AddWorkspaceDialog(_projectCollection))
            {
                if (dialog.Run(this) == Command.Ok)
                {
                    GetWorkspaces();
                }
            }
        }

        void RemoveWorkspaceClick(object sender, EventArgs e)
        {
            /*
            if (_listView.SelectedRow > -1 &&
                MessageService.Confirm(GettextCatalog.GetString("Are you sure you want to delete selected workspace?"), AlertButton.Yes))
            {
                var versionControl = _projectCollection.GetService<RepositoryService>();
                var name = _listStore.GetValue(_listView.SelectedRow, _name);
                var owner = _listStore.GetValue(_listView.SelectedRow, _owner);
                versionControl.DeleteWorkspace(name, owner);

                GetWorkspaces();
            }       
            */
        }
    }
}