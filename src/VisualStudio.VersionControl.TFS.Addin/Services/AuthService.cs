using System;
using VisualStudio.VersionControl.TFS.Addin.Helpers;

namespace VisualStudio.VersionControl.TFS.Addin.Services
{
    public class AuthService
    {
        public void SaveCredentials(string url, string password)
        {
            CredentialsHelper.StoreCredential(new Uri(url), password);   
        }
    }
}