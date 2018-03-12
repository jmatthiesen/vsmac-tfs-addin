using System;
using Microsoft.TeamFoundation.Client;
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;
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

        public ServerType ServerType { get { return _vstsRadioButton.Active ? ServerType.VSTS : ServerType.TFS; } }

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