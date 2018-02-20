using Microsoft.TeamFoundation.Client;

namespace MonoDevelop.VersionControl.TFS.Services
{
    public class ProjectService
    {
        public void LoadProjects(BaseTeamFoundationServer server)
        {
            server.LoadProjectConnections();
            server.ProjectCollections.ForEach(c => c.LoadProjects());
        }
    }
}