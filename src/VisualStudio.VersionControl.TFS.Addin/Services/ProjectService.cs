using Microsoft.TeamFoundation.Client;

namespace VisualStudio.VersionControl.TFS.Addin.Services
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