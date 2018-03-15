using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Gui.Widgets;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class AddServerDialog : Dialog
    {
        Notebook _notebook;
        AddServerWidget _addServerWidget;

        public AddServerDialog()
        {
            Init();
            BuildGui();
        }

        void Init()
        {
            _notebook = new Notebook();
            _addServerWidget = new AddServerWidget();
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Add Team Foundation Server");
            Buttons.Add(Command.Ok, Command.Cancel);
            _notebook.Add(_addServerWidget, GettextCatalog.GetString("Visual Studio Team Services"));
            Content = _notebook;
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