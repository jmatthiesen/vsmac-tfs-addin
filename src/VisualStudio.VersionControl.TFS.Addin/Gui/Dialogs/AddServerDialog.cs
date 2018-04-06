// AddServerDialog.cs
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

using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Gui.Widgets;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class AddServerDialog : Dialog
    {
        AddServerWidget _addServerWidget;

        public AddServerDialog()
        {
            Init();
            BuildGui();
        }

        void Init()
        {
            _addServerWidget = new AddServerWidget();
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Add Server");
           
            HBox buttonBox = new HBox
            {
                Margin = new WidgetSpacing(0, 12, 0, 12)
            };

            buttonBox.VerticalPlacement = WidgetPlacement.End;

            var cancelButton = new Button(GettextCatalog.GetString("Cancel"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            cancelButton.HorizontalPlacement = WidgetPlacement.Start;
            cancelButton.Clicked += (sender, e) => Respond(Command.Close);
            buttonBox.PackStart(cancelButton);

            _addServerWidget.PackStart(buttonBox);

            var acceptButton = new Button(GettextCatalog.GetString("Continue"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };
            acceptButton.HorizontalPlacement = WidgetPlacement.End;
            acceptButton.Clicked += (sender, e) => Respond(Command.Ok);
            buttonBox.PackEnd(acceptButton);

            Content = _addServerWidget;
            Resizable = false;
        }

        public AddServerResult Result
        {
            get
            {
                return _addServerWidget.Result;
            }
        }
    }
}