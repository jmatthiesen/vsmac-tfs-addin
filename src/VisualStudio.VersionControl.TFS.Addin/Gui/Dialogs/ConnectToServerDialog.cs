using System;
using System.Linq;
using Autofac;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class ConnectToServerDialog : Dialog
    {
        ListView _serverList;
        ListStore _serverStore;
        Notebook _notebook;

        readonly DataField<string> _nameField = new DataField<string>();
        readonly DataField<string> _urlField = new DataField<string>();
        readonly DataField<TeamFoundationServer> _serverField = new DataField<TeamFoundationServer>();

        TeamFoundationServerVersionControlService _service;

        public ConnectToServerDialog()
        {
            Init();
            BuildGui();
            UpdateServers();
        }

        void Init()
        {
            _service = DependencyInjection.Container.Resolve<TeamFoundationServerVersionControlService>();
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
            using (var addServerDialog = new AddServerDialog())
            {
                if (addServerDialog.Run(this) == Command.Ok)
                {
                    var addServerResult = addServerDialog.Result;

                    if (_service.HasServer(addServerResult.Url))
                    {
                        MessageService.ShowError("Server already exists!");
                        return;
                    }

                    using (var credentialsDialog = new CredentialsDialog(addServerResult.Url))
                    {
                        if (credentialsDialog.Run(this) == Command.Ok)
                        {
                            var serverAuthorization = credentialsDialog.Result;
                            var userPasswordAuthorization = serverAuthorization as UserPasswordAuthorization;
                           
                            if (userPasswordAuthorization != null && userPasswordAuthorization.ClearSavePassword)
                            {
                                MessageService.ShowWarning("No keyring service found!\nPassword will be saved as plain text.");
                            }

                            var server = TeamFoundationServer.FromAddServerDialog(addServerResult, serverAuthorization);
                         
                            using (var projectsDialog = new ChooseProjectsDialog(server))
                            {
                                if (projectsDialog.Run(this) == Command.Ok && projectsDialog.SelectedProjectColletions.Any())
                                {
                                    // Server has all project collections and projects, filter only sected.
                                    server.ProjectCollections.RemoveAll(pc => projectsDialog.SelectedProjectColletions.All(spc => spc != pc));
                                    foreach (var projectCollection in server.ProjectCollections)
                                    {
                                        var selectedProjectCollection = projectsDialog.SelectedProjectColletions.Single(spc => spc == projectCollection);
                                        projectCollection.Projects.RemoveAll(p => selectedProjectCollection.Projects.All(sp => sp != p));
                                    }

                                    _service.AddServer(server);
                                    UpdateServers();
                                }
                            }
                        }
                    }
                }
            }
        }

        void OnRemoveServer(object sender, EventArgs e)
        {
            if (MessageService.Confirm("Are you sure you want to delete this server!", AlertButton.Delete))
            {
                var serverUrl = _serverStore.GetValue(_serverList.SelectedRow, _urlField);
                _service.RemoveServer(new Uri(serverUrl));
                UpdateServers();
            }
        }

        void UpdateServers()
        {
            _serverStore.Clear();

            foreach (var server in _service.Servers)
            {
                var row = _serverStore.AddRow();

                _serverStore.SetValue(row, _nameField, server.Name);
                _serverStore.SetValue(row, _urlField, server.Uri.ToString());
                _serverStore.SetValue(row, _serverField, server);
            }
        }
    }
}