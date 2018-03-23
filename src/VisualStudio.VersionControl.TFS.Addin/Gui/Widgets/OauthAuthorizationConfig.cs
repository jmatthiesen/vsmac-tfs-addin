// OauthAuthorizationConfig.cs
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
using System.Diagnostics;
using System.Linq;
using Microsoft.IdentityService.Clients.ActiveDirectory;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    sealed class OauthAuthorizationConfig : UserPasswordAuthorizationConfig, IOAuthAuthorizationConfig
    {
        const string Authority = "https://login.microsoftonline.com/Common/oauth2/authorize";
        const string ClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        const string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        const string Resource = "https://management.core.windows.net";

        AuthenticationContext _context;

        Button _webViewButton;

        public OauthAuthorizationConfig(Uri serverUri)
            : base(serverUri)
        {
            Init();
            BuildGui();
            AttachEvents();
        }

        public string Token { get; set; }

        void Init()
        {
            UserPasswordContainer.Visible = false;

            _webViewButton = new Button(GettextCatalog.GetString("Sign in"));
        }

        void BuildGui()
        {
            _container.PackStart(_webViewButton);
        }

        void AttachEvents()
        {
            _webViewButton.Clicked += GetToken;
        }

        async void GetToken(object sender, EventArgs args)
        {
            try
            {
                TokenCache.DefaultShared.Clear();

                _context = new AuthenticationContext(Authority, true);

                var rootWindow = MessageService.RootWindow;
                var nsWindow = Components.Mac.GtkMacInterop.GetNSWindow(rootWindow);
                var platformParameter = new PlatformParameters(nsWindow);

                var result = await _context.AcquireTokenAsync(Resource, ClientId, new Uri(RedirectUri), platformParameter, UserIdentifier.AnyUser);

                Token = string.Empty;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}