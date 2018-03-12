using Microsoft.TeamFoundation.Client;

namespace MonoDevelop.VersionControl.TFS.Tests
{
    public static class TestSettings
    {
        internal static string Endpoint = "https://monodeveloptfs.visualstudio.com";
        internal static string Username = "monodeveloptfs@outlook.com";
        internal static string Password = "M0n0D3v3l0p";

        public static BaseServerInfo ServerInfo
        {
            get { return new VisualStudioServerInfo(Endpoint, Endpoint, Username); }
        }

        public static ServerAuthentication Auth
        {
            get
            {
                return new ServerAuthentication(ServerType.VSTS)
                {
                    AuthUser = Username,
                    Password = Password,
                    Domain = Endpoint
                };
            }
        }
    }
}