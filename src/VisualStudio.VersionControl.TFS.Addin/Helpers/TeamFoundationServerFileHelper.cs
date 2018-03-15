using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.VersionControl;
using MonoDevelop.VersionControl.TFS.VersionControl.Models;

namespace MonoDevelop.VersionControl.TFS.Helpers
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
