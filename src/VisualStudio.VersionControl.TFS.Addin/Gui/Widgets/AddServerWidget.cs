﻿// AddServerWidget.cs
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
using System.Text.RegularExpressions;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
    public class AddServerWidget : VBox
    {
        TextEntry _nameEntry;
        TextEntry _hostEntry;
        TextEntry _pathEntry;
        SpinButton _portEntry;
        RadioButtonGroup _protocolGroup;
        RadioButton _httpRadio;
        RadioButton _httpsRadio;
        TextEntry _previewEntry;
        TextEntry _userNameEntry;

        public AddServerWidget()
        {
            BuildGui();
        }

        void Init()
        {
            _nameEntry = new TextEntry();
            _hostEntry = new TextEntry();
            _pathEntry = new TextEntry();
            _portEntry = new SpinButton();
            _protocolGroup = new RadioButtonGroup();
            _httpRadio = new RadioButton(GettextCatalog.GetString("HTTP"));
            _httpsRadio = new RadioButton(GettextCatalog.GetString("HTTPS"));
            _previewEntry = new TextEntry();
            _userNameEntry = new TextEntry();
        }

        void BuildGui()
        {
            Margin = new WidgetSpacing(5, 5, 5, 5);
            PackStart(new Label(GettextCatalog.GetString("Name of connection")));
            _nameEntry.Changed += (sender, e) => _hostEntry.Text = _nameEntry.Text;
            PackStart(_nameEntry);

            PackStart(new Label(GettextCatalog.GetString("Host or URL of Team Foundation Server")));

            _hostEntry.Text = "";
            _hostEntry.Changed += OnUrlChanged;
            PackStart(_hostEntry);

            var tableDetails = new Table();
            tableDetails.Add(new Label(GettextCatalog.GetString("Connection Details")), 0, 0, 1, 2);
            tableDetails.Add(new Label(GettextCatalog.GetString("Path") + ":"), 0, 1);
            _pathEntry.Text = "tfs";
            _pathEntry.Changed += OnUrlChanged;
            tableDetails.Add(_pathEntry, 1, 1);

            tableDetails.Add(new Label(GettextCatalog.GetString("Port number") + ":"), 0, 2);
            _portEntry.MinimumValue = 1;
            _portEntry.MaximumValue = short.MaxValue;
            _portEntry.Value = 8080;
            _portEntry.IncrementValue = 1;
            _portEntry.Digits = 0;
            _portEntry.ValueChanged += OnUrlChanged;
            tableDetails.Add(_portEntry, 1, 2);

            tableDetails.Add(new Label(GettextCatalog.GetString("Protocol") + ":"), 0, 3);

            var protocolBox = new HBox();

            _httpRadio.Group = _protocolGroup;
            _httpRadio.Active = true;
            protocolBox.PackStart(_httpRadio);

            _httpsRadio.Group = _protocolGroup;
            _httpsRadio.Active = false;
            protocolBox.PackStart(_httpsRadio);

            _protocolGroup.ActiveRadioButtonChanged += (sender, e) =>
            {
                if (_protocolGroup.ActiveRadioButton == _httpRadio)
                {
                    _portEntry.Value = 8080;
                }
                else
                {
                    _portEntry.Value = 443;
                }
                BuildUrl();
            };

            tableDetails.Add(protocolBox, 1, 3);

            PackStart(tableDetails);

            var previewBox = new HBox();
            previewBox.PackStart(new Label(GettextCatalog.GetString("Preview") + ":"));
            previewBox.Sensitive = false;
            _previewEntry.BackgroundColor = Xwt.Drawing.Colors.LightGray;
            _previewEntry.MinWidth = 400;

            previewBox.PackStart(_previewEntry, true, true);
            PackStart(previewBox);

            var userNameBox = new HBox();
            userNameBox.PackStart(new Label(GettextCatalog.GetString("TFS user name") + ":"));
            userNameBox.PackStart(_userNameEntry, true, true);
            PackStart(userNameBox);

            BuildUrl();
        }

        void OnUrlChanged(object sender, EventArgs e)
        {
            BuildUrl();            
        }

        void BuildUrl()
        {
            if (string.IsNullOrWhiteSpace(_hostEntry.Text))
            {
                _previewEntry.Text = "Sever name cannot be empty.";
                return;
            }

            var hostIsUrl = Regex.IsMatch(_hostEntry.Text, "^http[s]?://");
            _pathEntry.Sensitive = !hostIsUrl;
            _portEntry.Sensitive = !hostIsUrl;
            _httpRadio.Sensitive = !hostIsUrl;
            _httpsRadio.Sensitive = !hostIsUrl;
        
            if (hostIsUrl)
            {
                _previewEntry.Text = _hostEntry.Text;
            }
            else
            {
                UriBuilder ub = new UriBuilder();
                ub.Host = _hostEntry.Text;
                ub.Scheme = _httpRadio.Active ? "http" : "https";
                ub.Port = (int)_portEntry.Value;
                ub.Path = _pathEntry.Text;
                _previewEntry.Text = ub.ToString();
            }
        }

        public AddServerResult Result
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_hostEntry.Text))
                    return null;
               
                var name = string.IsNullOrWhiteSpace(_nameEntry.Text) ? _previewEntry.Text : _nameEntry.Text;

                return new AddServerResult
                {
                    Name = name,
                    Url = new Uri(_previewEntry.Text),
                    UserName = _userNameEntry.Text
                };
            }
        }
    }
}