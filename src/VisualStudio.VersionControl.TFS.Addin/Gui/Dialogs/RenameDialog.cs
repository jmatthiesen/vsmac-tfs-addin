using System.IO;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
    public class RenameDialog : Dialog
    {
        ExtendedItem _item;
        TextEntry _nameEntry;

        internal RenameDialog(ExtendedItem item)
        {
            Init(item);
            BuildGui();
        }

        public string NewPath
        {
            get
            {
                var dir = Path.GetDirectoryName(_item.LocalPath);

                return Path.Combine(dir, _nameEntry.Text);
            }
        }

        void Init(ExtendedItem item)
        {
            _item = item;

            _nameEntry = new TextEntry();
        }

        void BuildGui()
        {
            var content = new HBox();
            content.PackStart(new Label(GettextCatalog.GetString("New name") + ":"));
            _nameEntry.Text = _item.ServerPath.ItemName;
            _nameEntry.WidthRequest = 250;
            content.PackStart(_nameEntry);

            Buttons.Add(Command.Ok, Command.Cancel);
           
            Content = content;
            Resizable = false;
        }
    }
}