using System;
using MonoDevelop.Core;
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

        public ConnectToServerDialog()
        {
            Init();
            BuildGui();
        }

        private void Init()
        {
            _serverList = new ListView();
            _notebook = new Notebook();
            _serverStore = new ListStore(_nameField, _urlField);
        }

        private void BuildGui()
        {
            this.Title = GettextCatalog.GetString("Add/Remove Team Foundation Server");
      
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

        void OnServerClicked(object sender, EventArgs e)
        {
            
        }

        void OnAddServer(object sender, EventArgs e)
        {
            using (var dialog = new AddServerDialog())
            {
                if (dialog.Run(this) == Command.Ok)
                {
             
                }
            }
        }

        void OnRemoveServer(object sender, EventArgs e)
        {
            
        }
    }
}