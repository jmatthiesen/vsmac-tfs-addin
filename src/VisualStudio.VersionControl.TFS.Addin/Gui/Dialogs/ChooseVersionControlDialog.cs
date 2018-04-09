// ChooseVersionControlDialog.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Gui.Cells;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public enum ServerType
    {
        VSTS,
        TFS
    }

    public class CellServerType
    {
        public Image Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ServerType ServerType { get; set; }
    }

    public class ChooseVersionControlDialog : Dialog
    {
        Label _title;
        VBox _listBox;
        ListView _listView;
        ListStore _store;
        DataField<CellServerType> _serverType;
        Button _cancelButton;
        Button _acceptButton;

        CellServerType _server;

        public ChooseVersionControlDialog()
        {
            Init();
            BuildGui();
            AttachEvents();
            GetData();
        }
          
        internal CellServerType Server
        {
            get { return _server; }
            set { _server = value; }
        }

        void Init()
        {
            _title = new Label(GettextCatalog.GetString("Where is your project hosted?"));
         
            _listBox = new VBox
            {
                MinHeight = 250
            };

            _listView = new ListView
            {
                Margin = new WidgetSpacing(24, 12, 24, 12),
                MinHeight = 96,
                BorderVisible = false,
                HeadersVisible = false,
                GridLinesVisible = GridLines.None
            };

            _serverType = new DataField<CellServerType>();
            _store = new ListStore(_serverType);
            _listView.Columns.Add("", new ServerTypeCellView { CellServerType = _serverType });
            _listView.DataSource = _store;

            _cancelButton = new Button(GettextCatalog.GetString("Cancel"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };
            _acceptButton = new Button(GettextCatalog.GetString("Continue"))
            {
                BackgroundColor = Colors.SkyBlue,
                MinWidth = GuiSettings.ButtonWidth
            };
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Open from Source Control");

            var content = new VBox
            {
                WidthRequest = 600
            };

            _listBox.PackStart(_listView);
            content.PackStart(_title);
            content.PackStart(_listBox);

            HBox buttonBox = new HBox
            {
                Margin = new WidgetSpacing(0, 0, 0, 12)
            };
                     
            _cancelButton.HorizontalPlacement = WidgetPlacement.Start;
            _cancelButton.Clicked += (sender, e) => Respond(Command.Close);
            buttonBox.PackStart(_cancelButton);

            content.PackStart(buttonBox);

            _acceptButton.HorizontalPlacement = WidgetPlacement.End;
            _acceptButton.Clicked += (sender, e) => Respond(Command.Ok);
            buttonBox.PackEnd(_acceptButton);

            Content = content;
            Resizable = false;
        }

        void AttachEvents()
        {
            _listView.SelectionChanged += OnSelectServer;
        }

        void GetData()
        {
            _store.Clear();

            foreach (var item in GetServers())
            {
                var row = _store.AddRow();
                _store.SetValue(row, _serverType, item);
            }
        }

        List<CellServerType> GetServers()
        {
            return new List<CellServerType>
            {
                new CellServerType { ServerType = ServerType.VSTS, Icon = Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.VSTS.png"), Title = "VSTS", Description = "A cloud service for code development collaboration by Microsoft" },
                new CellServerType { ServerType = ServerType.TFS, Icon = Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.TFS.png"), Title = "TFVC", Description = "Centralized Version Control by Microsoft" }
            };
        }

        void OnSelectServer(object sender, EventArgs args)
        {
            var row = _listView.SelectedRow;

            var serverType = _store.GetValue(row, _serverType);

            if (serverType != null)
            {
                Server = serverType;
            }        
        }
    }
}