using Microsoft.TeamFoundation.Client;
using VisualStudio.VersionControl.TFS.Addin.Services;

namespace VisualStudio.VersionControl.TFS.Addin
{
    public class TeamFoundationServerClient
    {
        static TeamFoundationServerClient _instance;
        static SettingsService _settings;

        AuthService _authService;
        ProjectService _projectService;

        public TeamFoundationServerClient()
        {
            _authService = new AuthService();
            _projectService = new ProjectService();
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
    }
}