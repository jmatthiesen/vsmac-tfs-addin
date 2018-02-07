using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs
{
    public class ConnectToServerDialog : Dialog
    {
        ListView _serverList;
        ListStore _serverStore;
        Notebook _notebook;

        readonly DataField<string> _nameField = new DataField<string>();
        readonly DataField<string> _urlField = new DataField<string>();
        readonly DataField<BaseTeamFoundationServer> _serverField = new DataField<BaseTeamFoundationServer>();

        public ConnectToServerDialog()
        {
            Init();
            BuildGui();
            UpdateServers();
        }

        void Init()
        {
            _serverList = new ListView();
            _notebook = new Notebook();
            _serverStore = new ListStore(_nameField, _urlField, _serverField);
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Add/Remove Team Foundation Server");
      
            var table = new Table();

            table.Add(new Label(GettextCatalog.GetString("Team Foundation Server list")), 0, 0, 1, 2);

            _serverList.SelectionMode = SelectionMode.Single;
            _serverList.MinWidth = 500;
            _serverList.MinHeight = 400;
            _serverList.Columns.Add(new ListViewColumn("Name", new TextCellView(_nameField) { Editable = false }));
            _serverList.Columns.Add(new ListViewColumn("Url", new TextCellView(_urlField) { Editable = false }));
            _serverList.DataSource = _serverStore;
            _serverList.RowActivated += OnServerClicked;
            table.Add(_serverList, 0, 1);

            VBox buttonBox = new VBox();
            var addButton = new Button(GettextCatalog.GetString("Add"));
            addButton.Clicked += OnAddServer;
            addButton.MinWidth = GuiSettings.ButtonWidth;
            buttonBox.PackStart(addButton);

            var removeButton = new Button(GettextCatalog.GetString("Remove"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            removeButton.Clicked += OnRemoveServer;
            buttonBox.PackStart(removeButton);

            var closeButton = new Button(GettextCatalog.GetString("Close"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            closeButton.Clicked += (sender, e) => Respond(Command.Close);
            buttonBox.PackStart(closeButton);

            table.Add(buttonBox, 1, 1);

            Content = table;
            Resizable = false;
        }

        void OnServerClicked(object sender, ListViewRowEventArgs e)
        {
  
        }

        void OnAddServer(object sender, EventArgs e)
        {
            using (var dialog = new AddServerDialog())
            {
                if (dialog.Run(this) == Command.Ok && dialog.ServerInfo != null)
                {
                    if (TeamFoundationServerClient.Settings.HasServer(dialog.ServerInfo.Name))
                    {
                        MessageService.ShowError("Server already exists!");
                        return;
                    }

                    var server = TeamFoundationServerClient.Instance.SaveCredentials(dialog.ServerInfo, dialog.ServerAuthentication);

                    using (var projectsDialog = new ChooseProjectsDialog(server))
                    {
                        if (projectsDialog.Run(this) == Command.Ok && projectsDialog.SelectedProjects.Any())
                        {
                            var selectedProjects = projectsDialog.SelectedProjects;
                            server.ProjectCollections = new List<ProjectCollection>(selectedProjects.Select(x => x.Collection).Distinct());
                            server.ProjectCollections.ForEach(pc => pc.Projects = new List<ProjectInfo>(selectedProjects.Where(pi => pi.Collection == pc)));
                            TeamFoundationServerClient.Settings.AddServer(server);
                            UpdateServers();
                        }
                    }
                }
            }
        }

        void OnRemoveServer(object sender, EventArgs e)
        {
            if (MessageService.Confirm("Are you sure you want to delete this server!", AlertButton.Delete))
            {
                var serverName = _serverStore.GetValue(_serverList.SelectedRow, _nameField);
                TeamFoundationServerClient.Settings.RemoveServer(serverName);
                UpdateServers();
            }
        }

        void UpdateServers()
        {
            _serverStore.Clear();

            foreach (var server in TeamFoundationServerClient.Settings.GetServers())
            {
                var row = _serverStore.AddRow();
                _serverStore.SetValue(row, _nameField, server.Name);
                _serverStore.SetValue(row, _urlField, server.Uri.ToString());
                _serverStore.SetValue(row, _serverField, server);
            }
        }
    }
}