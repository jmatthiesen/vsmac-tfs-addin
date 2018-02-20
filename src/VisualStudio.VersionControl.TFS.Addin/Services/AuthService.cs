using System;
using MonoDevelop.VersionControl.TFS.Helpers;

namespace MonoDevelop.VersionControl.TFS.Services
{
    public class AuthService
    {
        public void SaveCredentials(string url, string password)
        {
            CredentialsHelper.StoreCredential(new Uri(url), password);   
        }
    }
}