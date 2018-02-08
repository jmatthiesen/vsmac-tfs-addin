using System;
using Microsoft.TeamFoundation.Client;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using VisualStudio.VersionControl.TFS.Addin.Gui.Dialogs;
using Xwt;

namespace VisualStudio.VersionControl.TFS.Addin.Gui.Views
{
    public class SourceControlExplorerView : ViewContent 
    {
        ProjectCollection _projectCollection;

        VBox _view;
        Button _manageButton;

        public SourceControlExplorerView(ProjectCollection projectCollection)
        {
            _projectCollection = projectCollection;
            ContentName = GettextCatalog.GetString("Source Explorer");
            Init();
            BuildGui();
            AttachEvents();
        }

        public override Control Control => new XwtControl(_view);

        public static void Show(ProjectCollection projectCollection)
        {
            var sourceControlExplorerView = new SourceControlExplorerView(projectCollection);
            IdeApp.Workbench.OpenDocument(sourceControlExplorerView, true);
        }

        void Init()
        {
            _view = new VBox();
            _manageButton = new Button(GettextCatalog.GetString("Manage"));            
        }

        void BuildGui()
        {
            HBox headerBox = new HBox();
            headerBox.PackStart(_manageButton, false, false);
            _view.PackStart(headerBox, false, false);
        }

        void AttachEvents()
        {
            _manageButton.Clicked += OnManageWorkspaces;
        }

        void SetProjectCollection(ProjectCollection collection)
        {
            _projectCollection = collection;
        }

        void OnManageWorkspaces(object sender, EventArgs e)
        {
            using (var dialog = new WorkspacesDialog(_projectCollection))
            {
                if (dialog.Run() == Command.Close)
                {
                  
                }
            }
        }
    }
}