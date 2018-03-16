using System.Linq;
using System.Xml.Linq;
using Autofac;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS.Tests
{
    public class TestServer
    {
        internal TeamFoundationServer Server;

        public TestServer()
        {
            DependencyContainer.Register(new TestServiceBuilder());
            var versionControlService = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            if (!versionControlService.Servers.Any())
            {
                const string xmlConfig = @"
                <Server Name=""MonoDevelop"" Uri=""https://monodeveloptfs.visualstudio.com"" UserName=""monodeveloptfs@outlook.com"">
                     <Auth>
                          <Basic UserName=""monodeveloptfs@outlook.com"" Password=""M0n0D3v3l0p"" />
                     </Auth>
                </Server>";
                
                Server = TeamFoundationServer.FromConfigXml(XElement.Parse(xmlConfig));
            }
            else
            {
                Server = versionControlService.Servers.First();
            }

            Server.LoadStructure();
            versionControlService.AddServer(Server);
        }
    }
}