using System;
namespace VisualStudio.VersionControl.TFS.Addin.Models
{
    public class VisualStudioServerInfo
    {
        public VisualStudioServerInfo(string name, string url, string tfsUser, string tfsPassword)
        {
            Url = url;
            TFSUserName = tfsUser;
            TFSPassword = tfsPassword;
        }

        public string Url { get; set; }

        public string TFSUserName { get; set; }

        public string TFSPassword { get; set; }

        public Uri Uri
        {
            get
            {
                return new Uri(Url);
            }
        }
    }
}