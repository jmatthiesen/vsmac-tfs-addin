using Autofac;
using MonoDevelop.Components.Commands;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS.Commands
{
	public class InitCommand : CommandHandler
    {
        protected override void Run()
        {
			var service = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();

            // If any OAuth Token from cache is near to expire, renew it.
			service.RefreshAccessToken();
        }
    }
}