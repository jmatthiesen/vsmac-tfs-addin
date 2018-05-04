// ConnectToServerDialog.cs
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
using System.Linq;
using Autofac;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
	/// <summary>
    /// Connect to server dialog.
    /// </summary>
    public class ConnectToServerDialog : Dialog
    {
        ListView _serverList;
        ListStore _serverStore;
        Button _removeButton;
        
		DataField<string> _nameField;
		DataField<string> _urlField;
		DataField<string> _usernameField;
        DataField<TeamFoundationServer> _serverField;

        TeamFoundationServerVersionControlService _service;

        public ConnectToServerDialog()
        {
            Init();
            BuildGui();
			AttachEvents();
            UpdateServers();
            UpdateDeleteServer();
        }

        /// <summary>
		/// Init ConnectToServerDialog.
        /// </summary>
        void Init()
        {
            _service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();
            _serverList = new ListView();
			_nameField = new DataField<string>();
			_urlField = new DataField<string>();
			_usernameField = new DataField<string>();
			_serverField = new DataField<TeamFoundationServer>();
			_serverStore = new ListStore(_nameField, _urlField, _usernameField, _serverField);
        }

        /// <summary>
		/// Builds the ConnectToServerDialog GUI.
        /// </summary>
        void BuildGui()
        {
            Title = GettextCatalog.GetString("Add/Remove Server");
      
			var content = new VBox();

			content.PackStart(new Label(GettextCatalog.GetString("Servers")), true, true);
            
            _serverList.SelectionMode = SelectionMode.Single;
            _serverList.MinWidth = 600;
            _serverList.MinHeight = 250;
            _serverList.Columns.Add(new ListViewColumn("Name", new TextCellView(_nameField) { Editable = false }));
            _serverList.Columns.Add(new ListViewColumn("Url", new TextCellView(_urlField) { Editable = false }));
			_serverList.Columns.Add(new ListViewColumn("Username", new TextCellView(_usernameField) { Editable = false }));
            _serverList.DataSource = _serverStore;

			content.PackStart(_serverList, true, true);

            HBox buttonBox = new HBox();

            var addButton = new Button(GettextCatalog.GetString("Add"));
            addButton.Clicked += OnAddServer;
            addButton.MinWidth = GuiSettings.ButtonWidth;
            buttonBox.PackEnd(addButton);

            _removeButton = new Button(GettextCatalog.GetString("Remove"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };
            
			buttonBox.PackEnd(_removeButton);

            var closeButton = new Button(GettextCatalog.GetString("Close"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            closeButton.Clicked += (sender, e) => Respond(Command.Close);
            buttonBox.PackStart(closeButton);
            
			content.PackEnd(buttonBox, true, true);

			Content = content;
            Resizable = false;
        }


		void AttachEvents()
		{
            _serverList.SelectionChanged += (sender, args) => UpdateDeleteServer();
            _serverList.RowActivated += OnServerClicked;
			_removeButton.Clicked += OnRemoveServer;
		}

        void OnServerClicked(object sender, ListViewRowEventArgs e)
        {
            var serverConfig = _serverStore.GetValue(e.RowIndex, _serverField);
        
            using (var projectsDialog = new ChooseProjectsDialog(serverConfig))
            {
                if (projectsDialog.Run(this) == Command.Ok 
                    && projectsDialog.SelectedProjectColletions.Any())
                {
                    serverConfig.ProjectCollections.Clear();
                    serverConfig.ProjectCollections.AddRange(projectsDialog.SelectedProjectColletions);

                    // Notifty the changes
                    _service.ServersChange();
                }
            }
        }

        void OnAddServer(object sender, EventArgs e)
        {
            AddServer();
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

        /// <summary>
        /// Adds a server.
        /// </summary>
        void AddServer()
        {
            try
			{
				var toolkit = Toolkit.Load(ToolkitType.Gtk);

				toolkit.Invoke(() =>
				{
					using (var chooseVersionControlDialog = new ChooseVersionControlDialog())
					{
						if (chooseVersionControlDialog.Run(this) == Command.Ok)
						{
							chooseVersionControlDialog.Close();

							var serverType = chooseVersionControlDialog.Server;

							using (var credentialsDialog = new CredentialsDialog(serverType))
							{
								if (credentialsDialog.Run(this) == Command.Ok)
								{
									var serverAuthorization = credentialsDialog.ServerAuthorization;

									if (serverAuthorization is UserPasswordAuthorization userPasswordAuthorization && userPasswordAuthorization.ClearSavePassword)
									{
										MessageService.ShowWarning("No keyring service found!\nPassword will be saved as plain text.");
									}

									if (_service.HasServer(credentialsDialog.ServerUri))
									{
										MessageService.ShowError("Server already exists!");
										return;
									}

									var server = TeamFoundationServer.FromAddServerDialog(credentialsDialog.AddServerResult, serverAuthorization);

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
				});
            }
            catch
            {
                MessageService.ShowError("An error has occurred adding the server. Please try again later.");
            }
        }

        /// <summary>
        /// Updates the servers.
        /// </summary>
        void UpdateServers()
        {
            _serverStore.Clear();

            foreach (var server in _service.Servers)
            {
                var row = _serverStore.AddRow();

                _serverStore.SetValue(row, _nameField, server.Name);
                _serverStore.SetValue(row, _urlField, server.Uri.ToString());
				_serverStore.SetValue(row, _usernameField, server.UserName);
				_serverStore.SetValue(row, _serverField, server);
            }
        }
        /// <summary>
        /// Updates the delete server button state.
        /// </summary>
        void UpdateDeleteServer()
        {
            _removeButton.Sensitive = _serverList.SelectedRow != -1;
        }
    }
}