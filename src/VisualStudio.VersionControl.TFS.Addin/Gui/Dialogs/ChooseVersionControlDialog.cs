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
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class ChooseVersionControlDialog : Dialog
    {
        RadioButton _vstsRadioButton;
        RadioButton _tfsRadioButton;

        public ChooseVersionControlDialog()
        {
            Init();
            BuildGui();   
        }
          
        void Init()
        {
            _vstsRadioButton = new RadioButton();
            _vstsRadioButton.ActiveChanged += ChangeVSTSServerType;
            _vstsRadioButton.Active = true;
 
            _tfsRadioButton = new RadioButton();
            _tfsRadioButton.ActiveChanged += ChangeTFSServerType; 
            _tfsRadioButton.Active = false;     
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Version Control");

            var content = new VBox();
            content.WidthRequest = 500;

            Label titleLabel = new Label(GettextCatalog.GetString("Where is your project hosted?"));          
            content.PackStart(titleLabel);
     
            HBox vstsBox = new HBox();
            vstsBox.PackStart(_vstsRadioButton);

            Label vstsTitleLabel = new Label(GettextCatalog.GetString("Visual Studio Team Services"));
            vstsBox.PackStart(vstsTitleLabel);

            content.PackStart(vstsBox);

            Label vstsDescriptionLabel = new Label(GettextCatalog.GetString("A cloud service for code development collaboration by Microsoft"));
            vstsDescriptionLabel.Wrap = WrapMode.Word;
            content.PackStart(vstsDescriptionLabel);

            HBox tfsBox = new HBox();
            tfsBox.PackStart(_tfsRadioButton);
            Label tfsTitleLabel = new Label(GettextCatalog.GetString("Team Foundation Version Control"));
            tfsBox.PackStart(tfsTitleLabel);

            content.PackStart(tfsBox);

            Label tfsDescriptionLabel = new Label(GettextCatalog.GetString("Centralized Version Control system by Microsoft storing any type of artifac within its repository"));
            tfsDescriptionLabel.Wrap = WrapMode.Word;
            content.PackStart(tfsDescriptionLabel);

            HBox buttonBox = new HBox();
            buttonBox.HorizontalPlacement = WidgetPlacement.End;

            var acceptButton = new Button(GettextCatalog.GetString("Select"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            acceptButton.Clicked += (sender, e) => Respond(Command.Ok);
            buttonBox.PackStart(acceptButton);

            var closeButton = new Button(GettextCatalog.GetString("Cancel"))
            {
                MinWidth = GuiSettings.ButtonWidth
            };

            closeButton.Clicked += (sender, e) => Respond(Command.Close);
            buttonBox.PackStart(closeButton);

            content.PackStart(buttonBox);

            Content = content;
            Resizable = false;
        }

        void ChangeVSTSServerType(object sender, EventArgs args)
        {
            if(_vstsRadioButton.Active)
            {
                _tfsRadioButton.Active = false;
            }
        }

        void ChangeTFSServerType(object sender, EventArgs args)
        {
            if (_tfsRadioButton.Active)
            {
                _vstsRadioButton.Active = false;
            }
        }
    }
}