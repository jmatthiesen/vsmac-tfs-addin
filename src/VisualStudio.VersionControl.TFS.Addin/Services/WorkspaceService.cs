using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace VisualStudio.VersionControl.TFS.Addin.Services
{
    public class WorkspaceService
    {     
        public List<Workspace> GetLocalWorkspaces(ProjectCollection collection)
        {
            var versionControl = collection.GetService<RepositoryService>();
            return versionControl.QueryWorkspaces(collection.Server.UserName, Environment.MachineName);
        }

        public List<Workspace> GetRemoteWorkspaces(ProjectCollection collection)
        {
            var versionControl = collection.GetService<RepositoryService>();
            return versionControl.QueryWorkspaces(collection.Server.UserName, string.Empty);
        }

        public Workspace GetWorkspace(ProjectCollection collection, string workspaceName)
        {
            var versionControl = collection.GetService<RepositoryService>();
            return versionControl.QueryWorkspace(collection.Server.UserName, workspaceName);
        }
    }
}
