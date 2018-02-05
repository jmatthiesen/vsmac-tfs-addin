using VisualStudio.VersionControl.TFS.Addin.Models;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Widgets
{
    public class AddVisualStudioTeamServicesWidget : VBox
    {
        readonly TextEntry _urlEntry = new TextEntry();
        readonly TextEntry _tfsNameEntry = new TextEntry();
        readonly PasswordEntry _tfsPasswordEntry = new PasswordEntry();

        public AddVisualStudioTeamServicesWidget()
        {
            BuildGui();
        }

        void BuildGui()
        {
            Margin = new WidgetSpacing(5, 5, 5, 5);
            var tableDetails = new Table();
            tableDetails.Add(new Label("Visual Studio Team Services Url:"), 0, 1);
            tableDetails.Add(_urlEntry, 1, 1);
            tableDetails.Add(new Label("e.g. https://<<VSTS Name>>.visualstudio.com"), 2, 1);
            tableDetails.Add(new Label("TFS User:"), 0, 2);
            tableDetails.Add(_tfsNameEntry, 1, 2);
            tableDetails.Add(new Label("User name with access to TFS. Usually your Microsoft account."), 2, 2);
            tableDetails.Add(new Label("TFS Password:"), 0, 3);
            tableDetails.Add(_tfsPasswordEntry, 1, 3);
            tableDetails.Add(new Label("User password with access to TFS. Usually your Microsoft account password."), 2, 3);
            PackStart(tableDetails);
        }

        public VisualStudioServerInfo ServerInfo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_urlEntry.Text) || string.IsNullOrWhiteSpace(_tfsNameEntry.Text))
                    return null;

                var name = _urlEntry.Text;

                return new VisualStudioServerInfo(name, _urlEntry.Text, _tfsNameEntry.Text, _tfsPasswordEntry.Password);
            }
        }
    }
}