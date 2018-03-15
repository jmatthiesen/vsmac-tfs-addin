namespace MonoDevelop.VersionControl.TFS.Extensions
{
    public class TeamFoundationServerCommitDialogExtension : CommitDialogExtension
    {
        public override bool Initialize(ChangeSet changeSet)
        {
            var repo = changeSet.Repository as TeamFoundationServerRepository;

            if (repo != null)
            {
                return true;
            }

            return false;
        }

        public override bool OnBeginCommit(ChangeSet changeSet)
        {
            return true;
        }

        public override void OnEndCommit(ChangeSet changeSet, bool success)
        {
            base.OnEndCommit(changeSet, success);
        }
    }
}