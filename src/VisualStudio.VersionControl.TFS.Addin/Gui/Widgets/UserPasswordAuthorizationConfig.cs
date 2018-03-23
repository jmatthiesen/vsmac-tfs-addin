// UserPasswordAuthorizationConfig.cs
// 
// Authors:
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
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    class UserPasswordAuthorizationConfig : IServerAuthorizationConfig, IUserPasswordAuthorizationConfig
    {
        public VBox _container;
        VBox _userPasswordContainer;

        Uri _serverUri;
        TextEntry _domainEntry;
        TextEntry _userNameEntry;
        PasswordEntry _passwordEntry;
        bool? _clearSavePassword;

        public UserPasswordAuthorizationConfig(Uri serverUri)
        {
            Init(serverUri);
            BuildGui();
        }

        public Widget Widget
        {
            get 
            {
                return _container; 
            }
        }

        public VBox UserPasswordContainer
        {
            get
            {
                return _userPasswordContainer;
            }
        }

        public string UserName
        {
            get { return _userNameEntry.Text; }
        }

        public string Password
        {
            get
            {
                CredentialsManager.StoreCredential(_serverUri, _passwordEntry.Password);
                var password = CredentialsManager.GetPassword(_serverUri);
                _clearSavePassword = password != _passwordEntry.Password;

                return _passwordEntry.Password;
            }
        }

        public bool ClearSavePassword
        {
            get
            {
                if (!_clearSavePassword.HasValue)
                    throw new Exception("Get Password before getting this property");

                return _clearSavePassword.Value;
            }
        }

        void Init(Uri serverUri)
        {
            _serverUri = serverUri; 

            _container = new VBox();
            _userPasswordContainer = new VBox();
            _domainEntry = new TextEntry();
            _userNameEntry = new TextEntry();
            _passwordEntry = new PasswordEntry();
        }

        void BuildGui()
        {
            _userPasswordContainer.PackStart(new Label(GettextCatalog.GetString("User Name") + ":"));
            _userPasswordContainer.PackStart(_userNameEntry);
            _userPasswordContainer.PackStart(new Label(GettextCatalog.GetString("Password") + ":"));
            _userPasswordContainer.PackStart(_passwordEntry);
            _container.PackStart(_userPasswordContainer);
        }
    }
}