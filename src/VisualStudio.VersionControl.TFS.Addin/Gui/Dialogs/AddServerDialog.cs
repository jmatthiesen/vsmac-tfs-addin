using Microsoft.TeamFoundation.Client;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Gui.Widgets;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class AddServerDialog : Dialog
    {
        Notebook _notebook;
        AddVisualStudioTeamServicesWidget _vstsWidget;

        public AddServerDialog()
        {
            Init();
            BuildGui();
        }

        void Init()
        {
            _notebook = new Notebook();
            _vstsWidget = new AddVisualStudioTeamServicesWidget();
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Add Team Foundation Server");
            Buttons.Add(Command.Ok, Command.Cancel);
            _notebook.Add(_vstsWidget, GettextCatalog.GetString("Visual Studio Team Services"));
            Content = _notebook;
            Resizable = false;
        }

        public BaseServerInfo ServerInfo { get { return _vstsWidget.ServerInfo; } }

        public ServerAuthentication ServerAuthentication { get { return _vstsWidget.Authentication; } }
    }
}