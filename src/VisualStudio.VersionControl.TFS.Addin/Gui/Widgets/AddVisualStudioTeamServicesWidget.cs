using Microsoft.TeamFoundation.Client;
using MonoDevelop.Core;
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
            tableDetails.Add(new Label(GettextCatalog.GetString("Visual Studio Team Services Url:")), 0, 1);
            tableDetails.Add(_urlEntry, 1, 1);
            tableDetails.Add(new Label(GettextCatalog.GetString("e.g. https://<<VSTS Name>>.visualstudio.com")), 2, 1);
            tableDetails.Add(new Label(GettextCatalog.GetString("TFS User:")), 0, 2);
            tableDetails.Add(_tfsNameEntry, 1, 2);
            tableDetails.Add(new Label(GettextCatalog.GetString("User name with access to TFS. Usually your Microsoft account.")), 2, 2);
            tableDetails.Add(new Label(GettextCatalog.GetString("TFS Password:")), 0, 3);
            tableDetails.Add(_tfsPasswordEntry, 1, 3);
            tableDetails.Add(new Label(GettextCatalog.GetString("User password with access to TFS. Usually your Microsoft account password.")), 2, 3);
            PackStart(tableDetails);
        }

        public BaseServerInfo ServerInfo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_urlEntry.Text) || string.IsNullOrWhiteSpace(_tfsNameEntry.Text))
                    return null;

                var name = _urlEntry.Text;

                return new VisualStudioServerInfo(name, _urlEntry.Text, _tfsNameEntry.Text);
            }
        }

        public ServerAuthentication Authentication
        {
            get
            {
                if (string.IsNullOrEmpty(_tfsNameEntry.Text))
                    return null;
                
                var auth = new ServerAuthentication(ServerType.VisualStudio)
                {
                    AuthUser = _tfsNameEntry.Text,
                    Password = _tfsPasswordEntry.Password,
                    Domain = _urlEntry.Text
                };

                return auth;
            }
        }
    }
}