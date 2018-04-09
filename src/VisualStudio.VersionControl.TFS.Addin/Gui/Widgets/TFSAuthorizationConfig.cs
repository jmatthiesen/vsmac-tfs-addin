// TFSAuthorizationConfig.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    class TFSAuthorizationConfig : IServerAuthorizationConfig, IUserPasswordAuthorizationConfig
    {
        public VBox _container;
        VBox _tfsContainer;

        Label _titleLabel;
        TextEntry _nameEntry;
        TextEntry _serverEntry;
        TextEntry _userNameEntry;
        PasswordEntry _passwordEntry;
        TextEntry _domainEntry;
        Label _ntlmLabel;
        bool? _clearSavePassword;

        public TFSAuthorizationConfig()
        {
            Init();
            BuildGui();
        }

        public Widget Widget
        {
            get { return _container; }
        }

        public VBox TFSContainer
        {
            get { return _tfsContainer; }
        }

        public string UserName
        {
            get { return _userNameEntry.Text; }
        }

        public string Password
        {
            get
            {
                CredentialsManager.StoreCredential(new Uri(_serverEntry.Text), _passwordEntry.Password);
                var password = CredentialsManager.GetPassword(new Uri(_serverEntry.Text));
                _clearSavePassword = password != _passwordEntry.Password;

                return _passwordEntry.Password;
            }
        }

        public string Domain
        {
            get { return _domainEntry.Text ?? string.Empty; }
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

        public AddServerResult Result
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_serverEntry.Text))
                    return null;

                var name = string.IsNullOrWhiteSpace(_nameEntry.Text) ? _serverEntry.Text : _nameEntry.Text;

                return new AddServerResult
                {
                    Name = name,
                    Url = new Uri(_serverEntry.Text),
                    UserName = _userNameEntry.Text
                };
            }
        }

        void Init()
        {
            _container = new VBox();
            _tfsContainer = new VBox();

            _titleLabel = new Label(GettextCatalog.GetString("Connect to Team Foundation Server with your account credentials"));
            _nameEntry = new TextEntry
            {
                PlaceholderText = GettextCatalog.GetString("Name")
            };
            _serverEntry = new TextEntry
            {
                PlaceholderText = GettextCatalog.GetString("https://servername.com")
            };
            _userNameEntry = new TextEntry
            {
                PlaceholderText = GettextCatalog.GetString("Username")
            };
            _passwordEntry = new PasswordEntry
            {
                PlaceholderText = GettextCatalog.GetString("Password")
            };
            _domainEntry = new TextEntry
            {
                PlaceholderText = GettextCatalog.GetString("Domain")
            };
            _ntlmLabel = new Label(GettextCatalog.GetString("Optional, use for NTLM authentication"))
            {
                Font = Font.FromName(FontService.SansFontName).WithScaledSize(0.75),
                TextColor = Colors.Gray,
                Margin = new WidgetSpacing(70, 0, 0, 0)
            };
        }

        void BuildGui()
        {
            _container.PackStart(_titleLabel);

            HBox namebox = new HBox
            {
                Margin = new WidgetSpacing(72, 12, 48, 0)
            };
            Label nameLabel = new Label(GettextCatalog.GetString("Name:"))
            {
                HorizontalPlacement = WidgetPlacement.End,
                WidthRequest = 60
            };
            namebox.PackStart(nameLabel);
            namebox.PackStart(_nameEntry, true);
            _container.PackStart(namebox);

            HBox serverbox = new HBox
            {
                Margin = new WidgetSpacing(72, 0, 48, 0)
            };
            Label serverLabel = new Label(GettextCatalog.GetString("Server:"))
            {
                HorizontalPlacement = WidgetPlacement.End,
                WidthRequest = 60
            };
            serverbox.PackStart(serverLabel);
            serverbox.PackStart(_serverEntry, true);
            _container.PackStart(serverbox);

            HBox usernamebox = new HBox
            {
                Margin = new WidgetSpacing(72, 0, 48, 0)
            };
            Label usernameLabel = new Label(GettextCatalog.GetString("Username:"))
            {
                HorizontalPlacement = WidgetPlacement.End,
                WidthRequest = 60
            };
            usernamebox.PackStart(usernameLabel);
            usernamebox.PackStart(_userNameEntry, true);
            _container.PackStart(usernamebox);

            HBox passwordbox = new HBox
            {
                Margin = new WidgetSpacing(72, 0, 48, 0)
            };
            Label passwordLabel = new Label(GettextCatalog.GetString("Password:"))
            {
                HorizontalPlacement = WidgetPlacement.End,
                WidthRequest = 60
            };
            passwordbox.PackStart(passwordLabel);
            passwordbox.PackStart(_passwordEntry, true);
            _tfsContainer.PackStart(passwordbox);

            HBox domainbox = new HBox
            {
                Margin = new WidgetSpacing(72, 0, 48, 0)
            };
            Label domainLabel = new Label(GettextCatalog.GetString("Domain:"))
            {
                HorizontalPlacement = WidgetPlacement.End,
                WidthRequest = 60
            };

            domainbox.PackStart(domainLabel);
            domainbox.PackStart(_domainEntry, true);
            _tfsContainer.PackStart(domainbox);

            HBox ntlmbox = new HBox
            {
                Margin = new WidgetSpacing(72, 0, 48, 0)
            };
            ntlmbox.PackStart(_ntlmLabel);
            _tfsContainer.PackStart(ntlmbox);

            _container.PackStart(_tfsContainer);
        }
    }
}