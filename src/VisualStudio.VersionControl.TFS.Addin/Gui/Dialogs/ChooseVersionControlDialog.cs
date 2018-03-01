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

        void Init()
        {
            _vstsRadioButton = new RadioButton();
            _vstsRadioButton.Active = true;

            _tfsRadioButton = new RadioButton();
            _tfsRadioButton.Active = false;
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Version Control");

            var content = new VBox();
            content.HeightRequest = 200;
            content.WidthRequest = 600;

            Label titleLabel = new Label(GettextCatalog.GetString("Where is your project hosted?"));
            titleLabel.Font = FontService.SansFont.CopyModified(4).ToXwtFont();
            titleLabel.Margin = new WidgetSpacing(0, 12, 0, 12);
          
            content.PackStart(titleLabel);
     
            HBox vstsBox = new HBox();
            vstsBox.PackStart(_vstsRadioButton);
            Label vstsTitleLabel = new Label(GettextCatalog.GetString("Visual Studio Team Services"));
            vstsTitleLabel.Font = FontService.SansFont.CopyModified(2).ToXwtFont();
            vstsBox.PackStart(vstsTitleLabel);

            content.PackStart(vstsBox);

            Label vstsDescriptionLabel = new Label(GettextCatalog.GetString("A cloud service for code development collaboration by Microsoft"));
            vstsDescriptionLabel.Font = FontService.SansFont.CopyModified(1).ToXwtFont();
            vstsDescriptionLabel.Wrap = WrapMode.Word;
            content.PackStart(vstsDescriptionLabel);

            HBox tfsBox = new HBox();
            tfsBox.PackStart(_tfsRadioButton);
            Label tfsTitleLabel = new Label(GettextCatalog.GetString("Team Foundation Version Control"));
            tfsTitleLabel.Font = FontService.SansFont.CopyModified(2).ToXwtFont();
            tfsBox.PackStart(tfsTitleLabel);

            content.PackStart(tfsBox);

            Label tfsDescriptionLabel = new Label(GettextCatalog.GetString("Centralized Version Control system by Microsoft storing any type of artifac within its repository"));
            tfsDescriptionLabel.Font = FontService.SansFont.CopyModified(1).ToXwtFont();
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
    }
}