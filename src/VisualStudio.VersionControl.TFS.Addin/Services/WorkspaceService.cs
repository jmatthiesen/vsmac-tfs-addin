using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace MonoDevelop.VersionControl.TFS.Services
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

        public Workspace CreateWorkspace(RepositoryService repositoryService, Workspace workspace)
        {
            var createdWorkspace = repositoryService.CreateWorkspace(workspace);

            return createdWorkspace;
        }
    }
}
