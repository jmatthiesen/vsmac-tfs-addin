using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Ide.ProgressMonitoring;
using VisualStudio.VersionControl.TFS.Addin.Services;

namespace VisualStudio.VersionControl.TFS.Addin
{
    public class TeamFoundationServerClient
    {
        static TeamFoundationServerClient _instance;
        static SettingsService _settings;

        AuthService _authService;
        ProjectService _projectService;
        WorkspaceService _workspaceService;

        public TeamFoundationServerClient()
        {
            _authService = new AuthService();
            _projectService = new ProjectService();
            _workspaceService = new WorkspaceService();
        }

        public static TeamFoundationServerClient Instance { get { return _instance ?? (_instance = new TeamFoundationServerClient()); } }

        public static SettingsService Settings { get { return _settings ?? (_settings = new SettingsService()); } }

        public BaseTeamFoundationServer SaveCredentials(BaseServerInfo serverInfo, ServerAuthentication authentication)
        {
            try
            {
                _authService.SaveCredentials(serverInfo.Uri.OriginalString, authentication.Password);

                var server = TeamFoundationServerFactory.Create(
                    ServerType.VisualStudio,
                    serverInfo,
                    authentication,
                    false);
            
                return server;
            }
            catch
            {
                return null;
            }
        }

        public void LoadProjects(BaseTeamFoundationServer server)
        {
            _projectService.LoadProjects(server);
        }

        public List<Workspace> GetWorkspaces(ProjectCollection collection)
        {
            return _workspaceService.GetLocalWorkspaces(collection);
        }

        public string DownloadTempItem(Workspace workspace, ProjectCollection collection, ExtendedItem extendedItem)
        {
            var dowloadService = collection.GetService<VersionControlDownloadService>();
            var item = workspace.GetItem(extendedItem.ServerPath, ItemType.File, true);
            var filePath = dowloadService.DownloadToTempWithName(item.ArtifactUri, item.ServerPath.ItemName);

            return filePath;
        }

        public void Get(Workspace workspace, List<GetRequest> requests, GetOptions options, MessageDialogProgressMonitor monitor = null)
        {
            workspace.Get(requests, options, monitor);
        }

        public void GetLatestVersion(Workspace workspace, List<ExtendedItem> items, MessageDialogProgressMonitor monitor = null)
        {
            List<GetRequest> requests = new List<GetRequest>();

            foreach (var item in items)
            {
                RecursionType recursion = item.ItemType == ItemType.File ? RecursionType.None : RecursionType.Full;
                requests.Add(new GetRequest(item.ServerPath, recursion, VersionSpec.Latest));
            }

            var option = GetOptions.None;

            workspace.Get(requests, option, monitor);
        }

        public void Map(Workspace workspace, string serverPath, string localPath)
        {
            workspace.Map(serverPath, localPath);
        }

        public void LockFolders(Workspace workspace, List<string> paths, LockLevel lockLevel)
        {
            workspace.LockFolders(paths, lockLevel);
        }

        public void LockFiles(Workspace workspace, List<string> paths, LockLevel lockLevel)
        {
            workspace.LockFiles(paths, lockLevel);
        }

        public CheckInResult CheckIn(Workspace workspace, List<PendingChange> changes, string comment)
        {
            var result = workspace.CheckIn(changes, comment, null);

            return result;
        }

        public void PendRenameFile(Workspace workspace, string oldPath, string newPath, out List<Failure> failures)
        {
            workspace.PendRenameFile(oldPath, newPath, out failures);
        }

        public void PendRenameFolder(Workspace workspace, string oldPath, string newPath, out List<Failure> failures)
        {
            workspace.PendRenameFolder(oldPath, newPath, out failures);
        }
    }
}