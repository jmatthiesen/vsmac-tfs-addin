using System.Linq;
using Microsoft.TeamFoundation.Client;
using NUnit.Framework;

namespace MonoDevelop.VersionControl.TFS.Tests
{
    [TestFixture]
    public class ProjectCollectionTests
    {
        BaseTeamFoundationServer _server;

        public ProjectCollectionTests()
        {
            _server = TeamFoundationServerClient.Instance.SaveCredentials(
                TestSettings.ServerInfo, TestSettings.Auth, false);
        }

        [Test]
        public void LoadProjectTest()
        {
            TeamFoundationServerClient.Instance.LoadProjects(_server);

            var project = _server
                .ProjectCollections
                .SelectMany(pc => pc.Projects)
                .FirstOrDefault();

            Assert.NotNull(project);
            Assert.NotNull(project.Collection);
            Assert.NotNull(project.Collection.Server);
        }
    }
}
