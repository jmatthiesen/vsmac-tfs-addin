using System.Linq;
using NUnit.Framework;

namespace MonoDevelop.VersionControl.TFS.Tests
{
    [TestFixture]
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

            Assert.NotNull(project);
            Assert.NotNull(project.Collection);
            Assert.NotNull(project.Collection.Server);
        }
    }
}