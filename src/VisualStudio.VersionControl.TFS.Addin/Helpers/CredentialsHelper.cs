using System;
using MonoDevelop.Core;

namespace VisualStudio.VersionControl.TFS.Addin.Helpers
{
    public class CredentialsHelper
    {
        public static void StoreCredential(Uri url, string password)
        {
            PasswordService.AddWebPassword(url, password);
        }

        public static string GetPassword(Uri url)
        {
            try
            {
                return PasswordService.GetWebPassword(url);
            }
            catch
            {
                return null;
            }
        }
    }
}