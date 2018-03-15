namespace MonoDevelop.VersionControl.TFS.Models
{
    public sealed class CommitItem
    {
        public LocalPath LocalPath { get; set; }
        public RepositoryPath RepositoryPath { get; set; }
        public bool NeedUpload { get; set; }
    }
}