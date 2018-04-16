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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Gui.Widgets;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
	public enum ServerType
    {
        VSTS,
        TFS
    }

    public class ServerTypeInfo
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
		ProjectTypeWidget _vstsProjectTypeWidget;
		ProjectTypeWidget _tfsProjectTypeWidget;
        Button _cancelButton;
        Button _acceptButton;

		ServerTypeInfo _server;

        public ChooseVersionControlDialog()
        {
            Init();
            BuildGui();
			AttachEvents();
            GetData();
        }
          
		internal ServerTypeInfo Server
        {
            get { return _server; }
            set { _server = value; }
        }

        void Init()
        {
            _title = new Label(GettextCatalog.GetString("Where is your project hosted?"));
                     
            _listBox = new VBox
            {
				Margin = new WidgetSpacing(24, 24, 24, 0),
                MinHeight = 200
            };

			_vstsProjectTypeWidget = new ProjectTypeWidget
			{
				IsSelected = true
			};

			_tfsProjectTypeWidget = new ProjectTypeWidget
			{
				IsSelected = false
			};

			_cancelButton = new Button(GettextCatalog.GetString("Cancel"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            _acceptButton = new Button(GettextCatalog.GetString("Continue"))
            {
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
      
            content.PackStart(_title);   
			_listBox.PackStart(_vstsProjectTypeWidget);        
			_listBox.PackStart(_tfsProjectTypeWidget);
			content.PackStart(_listBox);   

			HBox buttonBox = new HBox
			{
				Margin = new WidgetSpacing(0, 12, 0, 0)
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
			_vstsProjectTypeWidget.ButtonPressed += OnServerTypeChange;
			_tfsProjectTypeWidget.ButtonPressed += OnServerTypeChange;
		}

		private void OnServerTypeChange(object sender, ButtonEventArgs e)
		{
			_vstsProjectTypeWidget.IsSelected = !_vstsProjectTypeWidget.IsSelected;
			_tfsProjectTypeWidget.IsSelected = !_tfsProjectTypeWidget.IsSelected;

			var servers = GetServers();

			if(_vstsProjectTypeWidget.IsSelected)
			{
				Server = servers.FirstOrDefault(s => s.ServerType == ServerType.VSTS);
			}
			else
			{
				Server = servers.FirstOrDefault(s => s.ServerType == ServerType.TFS);
			}
		}

		void GetData()
		{
			var servers = GetServers();

			var vstsServer = servers.First(s => s.ServerType == ServerType.VSTS);
			_vstsProjectTypeWidget.Icon = vstsServer.Icon;
			_vstsProjectTypeWidget.Title = vstsServer.Title;
			_vstsProjectTypeWidget.Description = vstsServer.Description;

			var tfsServer = servers.First(s => s.ServerType == ServerType.TFS);
			_tfsProjectTypeWidget.Icon = tfsServer.Icon;
			_tfsProjectTypeWidget.Title = tfsServer.Title;
			_tfsProjectTypeWidget.Description = tfsServer.Description;
		}

		List<ServerTypeInfo> GetServers()
        {
			return new List<ServerTypeInfo>
            {
				new ServerTypeInfo { ServerType = ServerType.VSTS, Icon = Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.VSTS.png"), Title = "VSTS", Description = "A cloud service for code development collaboration by Microsoft" },
				new ServerTypeInfo { ServerType = ServerType.TFS, Icon = Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.TFS.png"), Title = "TFVC", Description = "Centralized Version Control by Microsoft" }
            };
        }
    }
}