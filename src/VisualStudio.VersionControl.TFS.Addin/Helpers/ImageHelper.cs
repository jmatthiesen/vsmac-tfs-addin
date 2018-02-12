using Microsoft.TeamFoundation.VersionControl.Client.Enums;

namespace VisualStudio.VersionControl.TFS.Addin.Helpers
{
    public static class ImageHelper
    {
        public static Gdk.Pixbuf GetRepositoryImage()
        {
            return new Gdk.Pixbuf(System.Reflection.Assembly.GetCallingAssembly(), "VisualStudio.VersionControl.TFS.Addin.Icons.project-16.png", 16, 16);
        }

        public static Gdk.Pixbuf GetItemImage(ItemType itemType)
        {
            if (itemType == ItemType.File)
            {
                return new Gdk.Pixbuf(System.Reflection.Assembly.GetCallingAssembly(), "VisualStudio.VersionControl.TFS.Addin.Icons.file-16.png");
            }
            else
            {
                return new Gdk.Pixbuf(System.Reflection.Assembly.GetCallingAssembly(), "VisualStudio.VersionControl.TFS.Addin.Icons.folder-16.png");
            }
        }
    }
}