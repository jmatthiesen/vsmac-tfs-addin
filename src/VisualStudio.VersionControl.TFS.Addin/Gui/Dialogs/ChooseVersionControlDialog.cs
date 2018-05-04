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
	/// <summary>
	/// Server type. Options:
	/// - VSTS
	/// - TFS
    /// </summary>
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

    /// <summary>
    /// Choose version control dialog.
    /// </summary>
    public class ChooseVersionControlDialog : Dialog
    {
        Label _title;
        VBox _listBox;
		ServerTypeWidget _vstsProjectTypeWidget;
		ServerTypeWidget _tfsProjectTypeWidget;
        Button _cancelButton;
        Button _acceptButton;

		ServerTypeInfo _server;
		List<ServerTypeInfo> _servers;

        public ChooseVersionControlDialog()
        {
            Init();
            BuildGui();
			AttachEvents();
			LoadData();
        }
          
        /// <summary>
        /// Gets or sets the selected server.
        /// </summary>
        /// <value>The server.</value>
		internal ServerTypeInfo Server
        {
            get { return _server; }
            set { _server = value; }
        }

        /// <summary>
		/// Init ChooseVersionControlDialog.
        /// </summary>
        void Init()
        {
            _title = new Label(GettextCatalog.GetString("Where is your project hosted?"));
                     
            _listBox = new VBox
            {
				Margin = new WidgetSpacing(24, 24, 24, 0),
                MinHeight = 200
            };

			_vstsProjectTypeWidget = new ServerTypeWidget
			{
				IsSelected = true
			};

			_tfsProjectTypeWidget = new ServerTypeWidget
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

        /// <summary>
		/// Builds the ChooseVersionControlDialog GUI.
        /// </summary>
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

        /// <summary>
		/// Attachs the ChooseVersionControlDialog events.
        /// </summary>
		void AttachEvents()
        {
			_vstsProjectTypeWidget.ButtonPressed += OnServerTypeChange;
			_tfsProjectTypeWidget.ButtonPressed += OnServerTypeChange;
		}

        /// <summary>
        /// Ons the server type change.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
		void OnServerTypeChange(object sender, ButtonEventArgs e)
		{
			_vstsProjectTypeWidget.IsSelected = !_vstsProjectTypeWidget.IsSelected;
			_tfsProjectTypeWidget.IsSelected = !_tfsProjectTypeWidget.IsSelected;
         
			if(_vstsProjectTypeWidget.IsSelected)
			{
				Server = _servers.FirstOrDefault(s => s.ServerType == ServerType.VSTS);
			}
			else
			{
				Server = _servers.FirstOrDefault(s => s.ServerType == ServerType.TFS);
			}
		}

        /// <summary>
        /// Loads the data.
        /// </summary>
		void LoadData()
		{
			_servers = GetServers();

			var vstsServer = _servers.First(s => s.ServerType == ServerType.VSTS);
			_vstsProjectTypeWidget.Icon = vstsServer.Icon;
			_vstsProjectTypeWidget.Title = vstsServer.Title;
			_vstsProjectTypeWidget.Description = vstsServer.Description;

			var tfsServer = _servers.First(s => s.ServerType == ServerType.TFS);
			_tfsProjectTypeWidget.Icon = tfsServer.Icon;
			_tfsProjectTypeWidget.Title = tfsServer.Title;
			_tfsProjectTypeWidget.Description = tfsServer.Description;

			Server = _servers.FirstOrDefault(s => s.ServerType == ServerType.VSTS);
		}

        /// <summary>
		/// Gets the servers (VSTS and TFVC).
        /// </summary>
        /// <returns>The servers.</returns>
		List<ServerTypeInfo> GetServers()
        {
			return new List<ServerTypeInfo>
            {
                new ServerTypeInfo { ServerType = ServerType.VSTS, Icon = Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.VSTS.png"), Title = "Visual Studio Team Services", Description = "Visual Studio Team Services (VSTS) is a cloud service for collaborating on code development." },
				new ServerTypeInfo { ServerType = ServerType.TFS, Icon = Image.FromResource("MonoDevelop.VersionControl.TFS.Icons.TFS.png"), Title = "Team Foundation Server", Description = "" }
            };
        }
    }
}