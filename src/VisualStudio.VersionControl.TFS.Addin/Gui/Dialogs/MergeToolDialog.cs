using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class MergeToolDialog : Dialog
    {
        VBox _content;
        TextEntry _commandNameEntry;
        TextEntry _argumentsEntry;

        public MergeToolDialog(MergeToolInfo mergeInfo)
        {
            Init();
            BuildGui();
            GetData(mergeInfo);
        }

        public MergeToolInfo MergeToolInfo
        {
            get
            {
                if (string.IsNullOrEmpty(_commandNameEntry.Text))
                    return null;
                
                return new MergeToolInfo { CommandName = _commandNameEntry.Text, Arguments = _argumentsEntry.Text };
            }
        }

        void Init()
        {
            _content = new VBox();
            _commandNameEntry = new TextEntry();
            _argumentsEntry = new TextEntry();
        }

        void BuildGui()
        {
            Title = GettextCatalog.GetString("Merge Tool Config");
          
            var table = new Table();

            _commandNameEntry.Sensitive = false;
            _commandNameEntry.WidthRequest = 350;
            table.Add(_commandNameEntry, 1, 1);

            var commandSelectButton = new Button(GettextCatalog.GetString("Choose"));
            commandSelectButton.Clicked += (sender, e) => SelectCommand();
            commandSelectButton.WidthRequest = GuiSettings.ButtonWidth;
            table.Add(commandSelectButton, 2, 1);

            _argumentsEntry.TooltipText = GettextCatalog.GetString("Arguments");
            table.Add(_argumentsEntry, 1, 2, 1, 2);

            _content.PackStart(table);

            HBox buttonBox = new HBox();

            var buttonOk = new Button(GettextCatalog.GetString("Ok"));
            buttonOk.Clicked += (sender, e) => Respond(Command.Ok);
            var buttonCancel = new Button(GettextCatalog.GetString("Cancel"));
            buttonCancel.Clicked += (sender, e) => Respond(Command.Cancel);

            buttonOk.WidthRequest = buttonCancel.WidthRequest = GuiSettings.ButtonWidth;
            buttonBox.PackEnd(buttonOk);
            buttonBox.PackEnd(buttonCancel);
            _content.PackStart(buttonBox);

            Content = _content;
            Resizable = false;  
        }

        void GetData(MergeToolInfo mergeInfo)
        {
            if (mergeInfo != null)
            {
                _commandNameEntry.Text = mergeInfo.CommandName;
                _argumentsEntry.Text = mergeInfo.Arguments;
            }
        }

        void SelectCommand()
        {
            using (var fileSelect = new OpenFileDialog("Choose executable"))
            {
                fileSelect.Multiselect = false;

                if (fileSelect.Run(this))
                {
                    _commandNameEntry.TooltipText = _commandNameEntry.Text = fileSelect.FileName;
                }
            }
        }
    }
}