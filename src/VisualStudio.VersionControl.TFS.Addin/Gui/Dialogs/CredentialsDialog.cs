// CredentialsDialog.cs
// 
// Author:
//       Ventsislav Mladenov
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2018 Ventsislav Mladenov, Javier Suárez Ruiz
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
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Gui.Widgets;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class CredentialsDialog : Dialog
    {
        readonly Uri _serverUri;
        VBox _typeContainer;
        ServerAuthorizationType _serverAuthType;
        IServerAuthorizationConfig _currentConfig;

        public CredentialsDialog(Uri serverUri, CellProjectType serverType)
        {
            _serverUri = serverUri;
          
            Init();
            BuildGui(serverType);
        }

        void Init()
        {
            _typeContainer = new VBox();
        }

        void BuildGui(CellProjectType serverType)
        {
            Title = GettextCatalog.GetString("Credentials");

            VBox container = new VBox();

            SetTypeConfig(serverType);

            container.PackStart(_typeContainer);

            Buttons.Add(Command.Ok, Command.Cancel);
            Content = container;
        }


        void SetTypeConfig(CellProjectType serverType)
        {
            switch(serverType.ServerType)
            {
                case ServerType.VSTS:
                    _serverAuthType = ServerAuthorizationType.Oauth;
                    break;
                case ServerType.TFS:
                    _serverAuthType = ServerAuthorizationType.Basic;
                    break;
            }


            _currentConfig = ServerAuthorizationFactory.GetServerAuthorizationConfig(_serverAuthType, _serverUri);
            _typeContainer.Clear();
            _typeContainer.PackStart(_currentConfig.Widget, true, true);
        }

        internal IServerAuthorization Result
        {
            get
            {
                var type = _serverAuthType;
                return ServerAuthorizationFactory.GetServerAuthorization(type, _currentConfig);
            }
        }
    }
}