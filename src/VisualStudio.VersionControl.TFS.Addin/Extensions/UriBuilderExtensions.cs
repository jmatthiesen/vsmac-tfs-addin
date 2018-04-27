using System;
using System.IO;

namespace MonoDevelop.VersionControl.TFS.Extensions
{
    static class UriBuilderExtensions
    {
        internal static void AppendToPath(this UriBuilder builder, string pathToAdd)
        {
            var completePath = Path.Combine(builder.Path, pathToAdd);
            builder.Path = completePath;
        } 
    }
}