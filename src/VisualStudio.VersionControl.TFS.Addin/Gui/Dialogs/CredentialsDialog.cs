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
using System.Diagnostics;
using Microsoft.IdentityService.Clients.ActiveDirectory;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Gui.Widgets;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class CredentialsDialog : Dialog
    {
        VBox _typeContainer;
        ServerAuthorizationType _serverAuthType;
        IServerAuthorizationConfig _currentConfig;
        AuthenticationContext _context;
        readonly CellServerType _serverType;

        public CredentialsDialog(CellServerType serverType)
        {
            _serverType = serverType;

            Init();
            BuildGui();
        }

        internal IServerAuthorization ServerAuthorization
        {
            get
            {
                if (_currentConfig is VSTSAuthorizationConfig)
                {
                    var domain = ((VSTSAuthorizationConfig)_currentConfig).Domain;

                    if (!string.IsNullOrEmpty(domain))
                    {
                        _serverAuthType = ServerAuthorizationType.Ntlm;
                    }
                }

                return ServerAuthorizationFactory.GetServerAuthorization(_serverAuthType, _currentConfig);
            }
        }

        internal AddServerResult AddServerResult
        {
            get
            {
                if (_currentConfig is VSTSAuthorizationConfig)
                    return ((VSTSAuthorizationConfig)_currentConfig).Result;
                
                return ((TFSAuthorizationConfig)_currentConfig).Result;
            }
        }

        internal Uri ServerUri
        {
            get
            {
                if (_currentConfig is VSTSAuthorizationConfig)
                    return ((VSTSAuthorizationConfig)_currentConfig).Result.Url;
                
                return ((TFSAuthorizationConfig)_currentConfig).Result.Url;
            }
        }

        void Init()
        {
            _typeContainer = new VBox();
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Log In");

            VBox container = new VBox
            {
                MinWidth = 600
            };

            SetTypeConfig();

            container.PackStart(_typeContainer);

            HBox buttonBox = new HBox
            {
                Margin = new WidgetSpacing(0, 12, 0, 12)
            };

            var cancelButton = new Button(GettextCatalog.GetString("Cancel"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            cancelButton.HorizontalPlacement = WidgetPlacement.Start;
            cancelButton.Clicked += (sender, e) => Respond(Command.Cancel);
            buttonBox.PackStart(cancelButton);

            container.PackStart(buttonBox);

            var logInButton = new Button(GettextCatalog.GetString("Log in"))
            {
                BackgroundColor = Xwt.Drawing.Colors.SkyBlue,
                MinWidth = GuiSettings.ButtonWidth
            };

            logInButton.HorizontalPlacement = WidgetPlacement.End;
            logInButton.Clicked += OnLogIn;
            buttonBox.PackEnd(logInButton);

            var backButton = new Button(GettextCatalog.GetString("Back"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            backButton.HorizontalPlacement = WidgetPlacement.End;
            backButton.Clicked += (sender, e) => Respond(Command.Close);
            buttonBox.PackEnd(backButton);

            Content = container;
        }

        void SetTypeConfig()
        {
            switch(_serverType.ServerType)
            {
                case ServerType.VSTS:
                    _serverAuthType = ServerAuthorizationType.Oauth;
                    break;
                case ServerType.TFS:
                    _serverAuthType = ServerAuthorizationType.Basic;
                    break;
            }

            _currentConfig = ServerAuthorizationFactory.GetServerAuthorizationConfig(_serverAuthType);

            _typeContainer.Clear();
            _typeContainer.PackStart(_currentConfig.Widget, true, true);
        }

        async void OnLogIn(object sender, EventArgs args)
        {
            if(_serverType.ServerType == ServerType.VSTS)
            {
                try
                {
                    TokenCache.DefaultShared.Clear();

                    var serverResult = ((VSTSAuthorizationConfig)_currentConfig).Result;
                    var tokenCache = AdalCacheHelper.GetAdalFileCacheInstance(serverResult.Url.Host);

                    _context = new AuthenticationContext(OAuthConstants.Authority, true, tokenCache);

                    var rootWindow = MessageService.RootWindow;
                    var nsWindow = Components.Mac.GtkMacInterop.GetNSWindow(rootWindow);
                    var platformParameter = new PlatformParameters(nsWindow);

                    var authenticationResult = await _context.AcquireTokenAsync(OAuthConstants.Resource, OAuthConstants.ClientId, new Uri(OAuthConstants.RedirectUri), platformParameter, UserIdentifier.AnyUser);

                    ((VSTSAuthorizationConfig)_currentConfig).OauthToken = authenticationResult.AccessToken;
                    ((VSTSAuthorizationConfig)_currentConfig).ExpiresOn = authenticationResult.ExpiresOn;

                    AdalCacheHelper.PersistTokenCache(serverResult.Url.Host, tokenCache);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            Respond(Command.Ok);
        }
    }
}