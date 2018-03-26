using System.Linq;
using NUnit.Framework;

namespace MonoDevelop.VersionControl.TFS.Tests
{
    public class ProjectCollectionTests
    {
        TestServer _server;

        public ProjectCollectionTests()
        {
            _server = new TestServer();
        }

        [Test]
        public void LoadProjectTest()
        {
            var project = _server.Server
                .ProjectCollections
                .SelectMany(pc => pc.Projects)
                .FirstOrDefault();

            Assert.IsNotNull(project);
            Assert.IsNotNull(project.Collection);
            Assert.IsNotNull(project.Collection.Server);
        }
    }
}