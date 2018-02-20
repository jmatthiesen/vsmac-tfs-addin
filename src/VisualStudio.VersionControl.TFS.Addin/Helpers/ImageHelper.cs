using Microsoft.TeamFoundation.VersionControl.Client.Enums;

namespace MonoDevelop.VersionControl.TFS.Helpers
{
    public static class ImageHelper
    {
        public static Gdk.Pixbuf GetRepositoryImage()
        {
            return new Gdk.Pixbuf(System.Reflection.Assembly.GetCallingAssembly(), "MonoDevelop.VersionControl.TFS.Icons.project-16.png", 16, 16);
        }

        public static Gdk.Pixbuf GetItemImage(ItemType itemType)
        {
            if (itemType == ItemType.File)
            {
                return new Gdk.Pixbuf(System.Reflection.Assembly.GetCallingAssembly(), "MonoDevelop.VersionControl.TFS.Icons.file-16.png");
            }
            else
            {
                return new Gdk.Pixbuf(System.Reflection.Assembly.GetCallingAssembly(), "MonoDevelop.VersionControl.TFS.Icons.folder-16.png");
            }
        }
    }
}