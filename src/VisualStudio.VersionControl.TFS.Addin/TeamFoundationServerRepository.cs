// TeamFoundationServerRepository.cs
//
// Authors:
//       Ventsislav Mladenov
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2018 Ventsislav Mladenov, Javier Suárez Ruiz
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Gui.Views;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.MonoDevelopWrappers;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS
{
	/// <summary>
    /// Team Foundation Server repository.
    /// </summary>
    public class TeamFoundationServerRepository : Repository
    {
        readonly IWorkspaceService workspace;
        readonly VersionInfoResolver _versionInfoResolver;
        readonly TeamFoundationServerVersionControlService _versionControlService;
        readonly IFileKeeperService _fileKeeperService;

		public TeamFoundationServerRepository(
			string rootPath,
			IWorkspaceService workspace,
			TeamFoundationServerVersionControlService versionControlService,
			IFileKeeperService fileKeeperService)
		{
			if (workspace == null)
			{
				return;
			}

            RootPath = rootPath;
            this.workspace = workspace;
            _versionControlService = versionControlService;
            _fileKeeperService = fileKeeperService;

            _versionInfoResolver = new VersionInfoResolver(this);
        }

        /// <summary>
        /// Gets or sets the notification service.
        /// </summary>
        /// <value>The notification service.</value>
        public INotificationService NotificationService { get; set; }

        bool IsFileInWorkspace(LocalPath path)
        {      
            return workspace.Data.IsLocalPathMapped(path);
        }

        #region Implemented members of Repository

        #region Not Implemented

        protected override Repository OnPublish(string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor)
        {
            throw new NotSupportedException("Publish is not supported");
        }

        protected override void OnCheckout(FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor)
        {
            throw new NotSupportedException("CheckOut is not supported");
        }

        protected override void OnRevertRevision(FilePath localPath, Revision revision, ProgressMonitor monitor)
        {
			throw new NotSupportedException("Revert revision is not supported");
        }
        
        #endregion

        public override string GetBaseText(FilePath localFile)
        {
            var item = workspace.GetItem(ItemSpec.FromLocalPath(new LocalPath(localFile)), ItemType.File, true);
           
            return workspace.GetItemContent(item);
        }

        /// <summary>
        /// Get history.
        /// </summary>
        /// <returns>The get history.</returns>
        /// <param name="localFile">Local file.</param>
        /// <param name="since">Since.</param>
        protected override Revision[] OnGetHistory(FilePath localFile, Revision since)
        {
            var serverPath = workspace.Data.GetServerPathForLocalPath(new LocalPath(localFile));
            ItemSpec spec = new ItemSpec(serverPath, RecursionType.None);
            ChangesetVersionSpec versionFrom = null;

			if (since != null)
			{
				versionFrom = new ChangesetVersionSpec(((TeamFoundationServerRevision)since).Version);
			}

			return workspace.QueryHistory(spec, VersionSpec.Latest, versionFrom, null, short.MaxValue)
				            .Select(x => new TeamFoundationServerRevision(this, serverPath, x))
				            .ToArray();
        }
        
        /// <summary>
        /// Get version info.
        /// </summary>
        /// <returns>The get version info.</returns>
        /// <param name="paths">Paths.</param>
        /// <param name="getRemoteStatus">If set to <c>true</c> get remote status.</param>
        protected override IEnumerable<VersionInfo> OnGetVersionInfo(IEnumerable<FilePath> paths, bool getRemoteStatus)
        {
            var localPaths = paths.Select(p => new LocalPath(p));
          
            return _versionInfoResolver.GetFileStatus(localPaths).Values.ToArray();
        }

        /// <summary>
        /// Get directory version info.
        /// </summary>
        /// <returns>The get directory version info.</returns>
        /// <param name="localDirectory">Local directory.</param>
        /// <param name="getRemoteStatus">If set to <c>true</c> get remote status.</param>
        /// <param name="recursive">If set to <c>true</c> recursive.</param>
        protected override VersionInfo[] OnGetDirectoryVersionInfo(FilePath localDirectory, bool getRemoteStatus, bool recursive)
        {
            return _versionInfoResolver.GetDirectoryStatus(new LocalPath(localDirectory)).Values.ToArray();
        }

        /// <summary>
        /// Get latest version of files.
        /// </summary>
        /// <param name="localPaths">Local paths.</param>
        /// <param name="recurse">If set to <c>true</c> recurse.</param>
        /// <param name="monitor">Monitor.</param>
        protected override void OnUpdate(FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
        {
            var paths = localPaths.Select(x => new LocalPath(x)).ToArray();
          
			var getRequests = paths.Select(p => new LocalPath(p))
                                   .Where(IsFileInWorkspace)
                                   .Select(file => new GetRequest(file, recurse ? RecursionType.Full : RecursionType.None, VersionSpec.Latest))
                                   .ToList();
			
            workspace.Get(getRequests, GetOptions.None);
            _versionInfoResolver.InvalidateCache(paths);
            NotificationService.NotifyFilesChanged(paths);
        }
       
        /// <summary>
        /// Commit changes.
        /// </summary>
        /// <param name="changeSet">Change set.</param>
        /// <param name="monitor">Monitor.</param>
        protected override void OnCommit(ChangeSet changeSet, ProgressMonitor monitor)
        {         
            var commitItems = (from it in changeSet.Items
                let path = new LocalPath(it.LocalPath)
                let needUpload = path.IsFile && (it.Status.HasFlag(VersionStatus.ScheduledAdd) || it.Status.HasFlag(VersionStatus.Modified))
                select new CommitItem
                {
                    LocalPath = path,
                    NeedUpload = needUpload
                }).ToArray();

            Dictionary<int, WorkItemCheckinAction> workItems = null;

			if (changeSet.ExtendedProperties.Contains("TFS.WorkItems"))
			{
				workItems = (Dictionary<int, WorkItemCheckinAction>)changeSet.ExtendedProperties["TFS.WorkItems"];
			}

            var result = workspace.CheckIn(commitItems, changeSet.GlobalComment, workItems);
           
            if (result.Failures != null && result.Failures.Any(x => x.SeverityType == SeverityType.Error))
            {
                MessageService.ShowError("Commit failed!", string.Join(Environment.NewLine, result.Failures.Select(f => f.Message)));

                ResolveConflictsView.Open(workspace, changeSet.Items.Select(w => new LocalPath(w.LocalPath)).ToList());
            }

            foreach (var file in changeSet.Items.Where(i => !i.IsDirectory))
            {
                VersionControlService.NotifyFileStatusChanged(new FileUpdateEventArgs());
                NotificationService.NotifyFileChanged(file.LocalPath);
            }

            _versionInfoResolver.InvalidateCache(commitItems.Select(i => i.LocalPath));
        }

        /// <summary>
        /// Revert.
        /// </summary>
        /// <param name="localPaths">Local paths.</param>
        /// <param name="recurse">If set to <c>true</c> recurse.</param>
        /// <param name="monitor">Monitor.</param>
        protected override void OnRevert(FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
        {
            var specs = localPaths.Select(x => new ItemSpec(x, recurse ? RecursionType.Full : RecursionType.None));
           
			var operations = workspace.Undo(specs);
        
			_versionInfoResolver.InvalidateCache(operations);

            NotificationService.NotifyFilesChanged(operations);
            NotificationService.NotifyFilesRemoved(localPaths.Select(p => new LocalPath(p)).Where(p => !p.Exists));
        }

        /// <summary>
        /// Revert to revision.
        /// </summary>
        /// <param name="localPath">Local path.</param>
        /// <param name="revision">Revision.</param>
        /// <param name="monitor">Monitor.</param>
        protected override void OnRevertToRevision(FilePath localPath, Revision revision, ProgressMonitor monitor)
        {
            var spec = new ItemSpec(localPath, localPath.IsDirectory ? RecursionType.Full : RecursionType.None);
			var rev = (TeamFoundationServerRevision)revision;
            var request = new GetRequest(spec, new ChangesetVersionSpec(rev.Version));
          
			workspace.Get(request, GetOptions.None);

            _versionInfoResolver.InvalidateCache(localPath);
            NotificationService.NotifyFileChanged(localPath);
        }

        /// <summary>
        /// Add new item.
        /// </summary>
        /// <param name="localPaths">Local paths.</param>
        /// <param name="recurse">If set to <c>true</c> recurse.</param>
        /// <param name="monitor">Monitor.</param>
        protected override void OnAdd(FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
        {
            var paths = localPaths.Select(x => new LocalPath(x)).Where(IsFileInWorkspace).ToArray();
			workspace.PendAdd(paths, recurse, out ICollection<Failure> failures);

			if (failures.Any())
			{
				using (var failuresDialog = new FailuresDialog(failures))
				{
					failuresDialog.Run();
				}
			}

            _versionInfoResolver.InvalidateCache(paths, recurse);
            NotificationService.NotifyFilesChanged(paths);
        }

        /// <summary>
        /// Delete files.
        /// </summary>
        /// <param name="localPaths">Local paths.</param>
        /// <param name="force">If set to <c>true</c> force.</param>
        /// <param name="monitor">Monitor.</param>
        /// <param name="keepLocal">If set to <c>true</c> keep local.</param>
        protected override void OnDeleteFiles(FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
        {
            var paths = localPaths.Select(p => new LocalPath(p)).ToArray();
            DeletePaths(paths, false, monitor, keepLocal);
        }

        /// <summary>
        /// Delete directories.
        /// </summary>
        /// <param name="localPaths">Local paths.</param>
        /// <param name="force">If set to <c>true</c> force.</param>
        /// <param name="monitor">Monitor.</param>
        /// <param name="keepLocal">If set to <c>true</c> keep local.</param>
        protected override void OnDeleteDirectories(FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
        {
            var paths = localPaths.Select(p => new LocalPath(p)).ToArray();
            DeletePaths(paths, true, monitor, keepLocal);
        }

        void DeletePaths(LocalPath[] localPaths, bool recursive, ProgressMonitor monitor, bool keepLocal)
        {
            using (var keeper = _fileKeeperService.StartSession())
            {
                if (keepLocal) keeper.Save(localPaths, recursive);

                var statuses = _versionInfoResolver.GetFileStatus(localPaths);
               
                // Remove files which are versioned and not added.
                var forRemove = statuses.Where(s => s.Value.IsVersioned &&
                                                    !s.Value.HasLocalChange(VersionStatus.ScheduledAdd)).Select(s => s.Key);

				workspace.PendDelete(forRemove, recursive ? RecursionType.Full : RecursionType.None, keepLocal, out ICollection<Failure> failures);

				if (failures.Any(f => f.SeverityType == SeverityType.Error))
                {
                    foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
                    {
                        monitor.ReportError(failure.Code, new Exception(failure.Message));
                    }
                }

                // Revert added files.
                var addedFiles = statuses.Where(s => s.Value.HasLocalChange(VersionStatus.ScheduledAdd)).Select(s => new FilePath(s.Key)).ToArray();
                Revert(addedFiles, recursive, monitor);

                _versionInfoResolver.InvalidateCache(localPaths, recursive);
                NotificationService.NotifyFilesRemoved(localPaths);
            }
        }

        protected override string OnGetTextAtRevision(FilePath repositoryPath, Revision revision)
        {
			var teamFoundationServerRevision = (TeamFoundationServerRevision)revision;
           
			if (teamFoundationServerRevision.Version == 0)
                return string.Empty;

            var serverPath = workspace.Data.GetServerPathForLocalPath(new LocalPath(repositoryPath));
          
            if (string.IsNullOrEmpty(serverPath))
                return string.Empty;

            var item = new ItemSpec(serverPath, RecursionType.None);
			var items = workspace.GetItems(item.ToEnumerable(), new ChangesetVersionSpec(teamFoundationServerRevision.Version), DeletedState.Any, ItemType.Any, true);

			if (items.Count == 0)
			{
				return string.Empty;
			}

            return workspace.GetItemContent(items[0]);
        }

        /// <summary>
        /// Get revision changes.
        /// </summary>
        /// <returns>The get revision changes.</returns>
        /// <param name="revision">Revision.</param>
        protected override RevisionPath[] OnGetRevisionChanges(Revision revision)
        {
			var teamFoundationServerRevision = (TeamFoundationServerRevision)revision;

			var changeSet = workspace.QueryChangeset(teamFoundationServerRevision.Version, true);
            List<RevisionPath> revisionPaths = new List<RevisionPath>();
          
            var changesByItemGroup = from ch in changeSet.Changes
                                              group ch by ch.Item into grCh
                                              select grCh;
            
            foreach (var changesByItem in changesByItemGroup)
            {
                RevisionAction action;
                var changes = changesByItem.Select(ch => ch.ChangeType).ToList();
             
                if (changes.Any(ch => ch.HasFlag(ChangeType.Add)))
                    action = RevisionAction.Add;
                else if (changes.Any(ch => ch.HasFlag(ChangeType.Delete)))
                    action = RevisionAction.Delete;
                else if (changes.Any(ch => ch.HasFlag(ChangeType.Rename) || ch.HasFlag(ChangeType.SourceRename)))
                    action = RevisionAction.Replace;
                else if (changes.Any(ch => ch.HasFlag(ChangeType.Edit) || ch.HasFlag(ChangeType.Encoding)))
                    action = RevisionAction.Modify;
                else
                    action = RevisionAction.Other;
             
                string path = workspace.Data.GetLocalPathForServerPath(changesByItem.Key.ServerPath);
                path = string.IsNullOrEmpty(path) ? changesByItem.Key.ServerItem : path;
                revisionPaths.Add(new RevisionPath(path, action, action.ToString()));
            }

            return revisionPaths.ToArray();
        }

        protected override void OnIgnore(FilePath[] localPath)
        {
            //TODO: Add Ignore Option
        }

        protected override void OnUnignore(FilePath[] localPath)
        {
            //TODO: Add UnIgnore Option
        }

        /// <summary>
        /// Generates the diff.
        /// </summary>
        /// <returns>The diff.</returns>
        /// <param name="baseLocalPath">Base local path.</param>
        /// <param name="versionInfo">Version info.</param>
        public override DiffInfo GenerateDiff(FilePath baseLocalPath, VersionInfo versionInfo)
        {
            if (versionInfo.LocalPath.IsDirectory)
                return null;
            
            string text;
            if (versionInfo.Status.HasFlag(VersionStatus.ScheduledAdd) || versionInfo.Status.HasFlag(VersionStatus.ScheduledDelete))
            {
                if (versionInfo.Status.HasFlag(VersionStatus.ScheduledAdd))
                {
                    var lines = File.ReadAllLines(versionInfo.LocalPath);
                    text = string.Join(Environment.NewLine, lines.Select(l => "+" + l));
                  
					return new DiffInfo(baseLocalPath, versionInfo.LocalPath, text);
                }

                if (versionInfo.Status.HasFlag(VersionStatus.ScheduledDelete))
                {
                    var item = workspace.GetItem(ItemSpec.FromServerPath(new RepositoryPath(versionInfo.RepositoryPath, false)), ItemType.File, true);
                    var lines = workspace.GetItemContent(item).Split(new [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    text = string.Join(Environment.NewLine, lines.Select(l => "-" + l));
                   
					return new DiffInfo(baseLocalPath, versionInfo.LocalPath, text);
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Move file.
        /// </summary>
        /// <param name="localSrcPath">Local source path.</param>
        /// <param name="localDestPath">Local destination path.</param>
        /// <param name="force">If set to <c>true</c> force.</param>
        /// <param name="monitor">Monitor.</param>
        protected override void OnMoveFile(FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
        {
            base.OnMoveFile(localSrcPath, localDestPath, force, monitor);
            Move(new LocalPath(localSrcPath), new LocalPath(localDestPath));
        }

        /// <summary>
        /// Move directory.
        /// </summary>
        /// <param name="localSrcPath">Local source path.</param>
        /// <param name="localDestPath">Local destination path.</param>
        /// <param name="force">If set to <c>true</c> force.</param>
        /// <param name="monitor">Monitor.</param>
        protected override void OnMoveDirectory(FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
        {
            base.OnMoveDirectory(localSrcPath, localDestPath, force, monitor);
            Move(new LocalPath(localSrcPath), new LocalPath(localDestPath));
        }

        /// <summary>
        /// Move or rename a file or folder.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        void Move(LocalPath from, LocalPath to)
        {
			if (workspace != null)
			{
				workspace.PendRename(from, to, out ICollection<Failure> failures);

				if (failures.Any())
				{
					using (var failuresDialog = new FailuresDialog(failures))
					{
						failuresDialog.Run();
					}
				}

				_versionInfoResolver.InvalidateCache(to);

				NotificationService.NotifyFileRemoved(from);
				NotificationService.NotifyFileChanged(to);
			}
        }

        /// <summary>
        /// Lock.
        /// </summary>
        /// <param name="monitor">Monitor.</param>
        /// <param name="localPaths">Local paths.</param>
        protected override void OnLock(ProgressMonitor monitor, params FilePath[] localPaths)
        {
            var paths = localPaths.Select(x => new LocalPath(x)).ToArray();
            LockItems(paths, LockLevel.CheckOut);
        }
        
        /// <summary>
        /// Unlock.
        /// </summary>
        /// <param name="monitor">Monitor.</param>
        /// <param name="localPaths">Local paths.</param>
        protected override void OnUnlock(ProgressMonitor monitor, params FilePath[] localPaths)
        {
            var paths = localPaths.Select(x => new LocalPath(x)).ToArray();
            LockItems(paths, LockLevel.None);
        }
          
        /// <summary>
        /// Lock the specific items.
        /// </summary>
        /// <param name="localPaths">Local paths.</param>
        /// <param name="lockLevel">Lock level.</param>
        void LockItems(LocalPath[] localPaths, LockLevel lockLevel)
        {
            workspace.LockItems(localPaths, lockLevel);
            _versionInfoResolver.InvalidateCache(localPaths);
            NotificationService.NotifyFilesChanged(localPaths);
        }

        /// <summary>
        /// Gets the supported operations.
        /// </summary>
        /// <returns>The supported operations.</returns>
        /// <param name="vinfo">Vinfo.</param>
        protected override VersionControlOperation GetSupportedOperations(VersionInfo vinfo)
        {
            if (!IsFileInWorkspace(new LocalPath(vinfo.LocalPath)))
            {
                return VersionControlOperation.None;
            }

            var supportedOperations = base.GetSupportedOperations(vinfo);
           
            if (vinfo.HasLocalChanges) //Disable update for modified files.
                supportedOperations &= ~VersionControlOperation.Update;
            supportedOperations &= ~VersionControlOperation.Annotate; //Annotated is not supported yet.
          
            if (vinfo.Status.HasFlag(VersionStatus.ScheduledAdd))
                supportedOperations &= ~VersionControlOperation.Log;

            if (vinfo.Status.HasFlag(VersionStatus.Locked))
            {
                supportedOperations &= ~VersionControlOperation.Lock;
                supportedOperations &= ~VersionControlOperation.Remove;
            }

            return supportedOperations;
        }

        /// <summary>
		/// Requests the file write permission (modify an existing file).
        /// </summary>
        /// <returns><c>true</c>, if file write permission was requested, <c>false</c> otherwise.</returns>
        /// <param name="paths">Paths.</param>
        public override bool RequestFileWritePermission(params FilePath[] paths)
        {                   
            using (var progress = VersionControlService.GetProgressMonitor("Edit"))
            {
                foreach (var path in paths.Select(p => new LocalPath(p)))
                {
                    if (!path.Exists || !path.IsReadOnly)
                        continue;
                    
                    progress.Log.WriteLine("Start editing item: " + path);
                  
                    try
                    {
						workspace.PendEdit(path.ToEnumerable(), RecursionType.None, _versionControlService.CheckOutLockLevel, out ICollection<Failure> failures);

						if (failures.Any(f => f.SeverityType == SeverityType.Error))
                        {
                            foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
                            {
                                progress.ReportError(failure.Code, new Exception(failure.Message));
                            }
                        }
                        else
                        {
                            _versionInfoResolver.InvalidateCache(path);
                            NotificationService.NotifyFileChanged(path);
                            progress.ReportSuccess("Finish editing item.");
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.ReportError(ex.Message, ex);
                        return false;
                    }
                }
            }           
         
            return true;
        }

        public override bool AllowLocking { get { return true; } }

        public override bool AllowModifyUnlockedFiles { get { return true; } }
        
        public override bool SupportsRemoteStatus { get { return false; } }

        #endregion

        /// <summary>
        /// Checkouts a specific file.
        /// </summary>
        /// <param name="path">Path.</param>
        internal void CheckoutFile(LocalPath path)
        {
            using (var progress = VersionControlService.GetProgressMonitor("CheckOut"))
            {
                progress.Log.WriteLine("Start check out item: " + path);
				workspace.CheckOut(path.ToEnumerable(), out ICollection<Failure> failures);

				if (failures.Any())
				{
					using (var failuresDialog = new FailuresDialog(failures))
					{
						failuresDialog.Run();
					}
				}

                _versionInfoResolver.InvalidateCache(path);
                NotificationService.NotifyFileChanged(path);
                progress.ReportSuccess("Finish check out.");
            }
        }
        
        /// <summary>
        /// Refresh cache.
        /// </summary>
        internal void Refresh()
        {
            _versionInfoResolver.InvalidateCache();
        }

        internal IWorkspaceService Workspace { get { return workspace; }}
    }
}