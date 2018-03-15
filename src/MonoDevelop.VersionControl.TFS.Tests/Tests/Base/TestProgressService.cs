using System;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS.Tests
{
    internal sealed class TestProgressService : IProgressService
    {
        public IProgressDisplay CreateProgress()
        {
            return new TestProgressDisplay();
        }
    }
}