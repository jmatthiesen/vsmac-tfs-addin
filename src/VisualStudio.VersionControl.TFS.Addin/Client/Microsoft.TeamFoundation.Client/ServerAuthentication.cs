namespace Microsoft.TeamFoundation.Client
{
    public class ServerAuthentication
    {
        ServerType serverType;

        public ServerAuthentication(ServerType serverType)
        {
            this.serverType = serverType;
            
        }

        public string AuthUser { get; set; }

        public string Password { get; set; }

        string domain;
        public string Domain 
        { 
            get
            {
                if (serverType != ServerType.TFS)
                    return string.Empty;
                return domain;
            } 
            set
            {
                domain = value;
            }
        }

        string userName;
        public string UserName 
        { 
            get
            {
                if (serverType == ServerType.TFS)
                    return AuthUser;
                
                return userName;
            }
            set
            {
                userName = value;
            }
        }
    }
}