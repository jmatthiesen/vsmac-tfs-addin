using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Core;

namespace VisualStudio.VersionControl.TFS.Addin.Helpers
{
    public class TeamFoundationServerFileHelper
    {
        public static void NotifyFilesChanged(Workspace workspace, List<ExtendedItem> items)
        {
            FileService.NotifyFilesChanged(items.Select(i => (FilePath)workspace.GetLocalPathForServerPath(i.ServerPath)), true);
        }

        public static void NotifyFilesRemoved(Workspace workspace, List<ExtendedItem> items)
        {
            FileService.NotifyFilesRemoved(items.Select(i => (FilePath)workspace.GetLocalPathForServerPath(i.ServerPath)));
        }
    }
}
