using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Pads
{
    public class TeamExplorerPad : PadContent
    {
        VBox _content;

        public override Control Control { get { return new XwtControl(_content); } }

        protected override void Initialize(IPadWindow container)
        {
            base.Initialize(container);

            _content = new VBox();
        }
    }
}