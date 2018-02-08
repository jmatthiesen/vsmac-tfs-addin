using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs
{
    public class AddWorkspaceDialog : Dialog
    {
        ListView _foldersView;
        DataField<string> _tfsFolder;
        DataField<string> _localFolder;
        ListStore _foldersStore;

        public AddWorkspaceDialog()
        {
            Init();
            BuildGui();
        }

        void Init()
        {
            _foldersView = new ListView();    
            _tfsFolder = new DataField<string>();
            _localFolder = new DataField<string>();
            _foldersStore = new ListStore(_tfsFolder, _localFolder);
        }

        void BuildGui()
        {
            VBox content = new VBox();

            Content = content;
            Resizable = false;
        }
    }
}
