using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
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

        public void GetLatestVersion(Workspace workspace, List<ExtendedItem> items)
        {
            List<GetRequest> requests = new List<GetRequest>();

            foreach (var item in items)
            {
                RecursionType recursion = item.ItemType == ItemType.File ? RecursionType.None : RecursionType.Full;
                requests.Add(new GetRequest(item.ServerPath, recursion, VersionSpec.Latest));
            }

            var option = GetOptions.None;

            workspace.Get(requests, option);
        }
    }
}