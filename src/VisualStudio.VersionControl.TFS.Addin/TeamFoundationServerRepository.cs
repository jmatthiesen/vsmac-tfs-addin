using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Gui.Dialogs;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS
{
    public class TeamFoundationServerRepository : Repository
    {
        List<Workspace> _workspaces;
        RepositoryCache _cache;

        internal TeamFoundationServerRepository(RepositoryService versionControlService, string rootPath)
        {
            VersionControlService = versionControlService;
            RootPath = rootPath;  

            _workspaces = new List<Workspace>();
            _cache = new RepositoryCache(this);
        }

        internal RepositoryService VersionControlService { get; set; }

        public override string GetBaseText(FilePath localFile)
        {
            var workspace = GetWorkspaceByLocalPath(localFile);
            var serverPath = workspace.GetServerPathForLocalPath(localFile);
         
            if (string.IsNullOrEmpty(serverPath))
                return string.Empty;

            var item = workspace.GetItem(serverPath, ItemType.File, true);

            return workspace.GetItemContent(item);
        }

        public override bool RequestFileWritePermission(params FilePath[] paths)
        {
            using (var progress = VersionControl.VersionControlService.GetProgressMonitor("Edit"))
            {
                foreach (var path in paths)
                {
                    if (!File.Exists(path) || !File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly))
                        continue;
                  
                    progress.Log.WriteLine("Start editing item: " + path);
                   
                    try
                    {
                        var workspace = GetWorkspaceByLocalPath(path);
                        var failures = workspace.PendEdit(new List<FilePath> { path }, RecursionType.None, TeamFoundationServerClient.Settings.CheckOutLockLevel);
                       
                        if (failures.Any(f => f.SeverityType == SeverityType.Error))
                        {
                            foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
                            {
                                progress.ReportError(failure.Code, new Exception(failure.Message));
                            }
                        }
                        else
                        {
                            _cache.RefreshItem(path);
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

        public void Resolve(Conflict conflict, ResolutionType resolutionType)
        {
            conflict.Workspace.Resolve(conflict, resolutionType);
        }

        protected override void OnAdd(FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
        {
            foreach (var ws in GroupFilesPerWorkspace(localPaths))
            {
                TeamFoundationServerClient.Instance.PendAdd(ws.Key, ws.ToList(), recurse);
            }

            _cache.RefreshItems(localPaths);
            FileService.NotifyFilesChanged(localPaths);
        }

        protected override void OnCheckout(FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor)
        {
            var workspaces = TeamFoundationServerClient.Instance.GetWorkspaces(VersionControlService.Collection);
           
            if (workspaces.Count == 0)
            {
                Workspace newWorkspace = new Workspace(VersionControlService,
                                                       Environment.MachineName + ".WS", VersionControlService.Collection.Server.UserName, "Auto created",
                                                       new List<WorkingFolder> { new WorkingFolder(VersionControlPath.RootFolder, targetLocalPath) }, Environment.MachineName);
               
                var workspace = TeamFoundationServerClient.Instance.CreateWorkspace(VersionControlService, newWorkspace);
              
                TeamFoundationServerClient.Instance.Get(workspace, new GetRequest(VersionControlPath.RootFolder, RecursionType.Full, VersionSpec.Latest), GetOptions.None);
            }
            else
            {
                _workspaces.AddRange(workspaces);
                var workspace = GetWorkspaceByLocalPath(targetLocalPath);
              
                if (workspace == null)
                    return;
                
                TeamFoundationServerClient.Instance.Get(workspace, new GetRequest(workspace.GetServerPathForLocalPath(targetLocalPath), RecursionType.Full, VersionSpec.Latest), GetOptions.None);
            }
        }

        protected override void OnCommit(ChangeSet changeSet, ProgressMonitor monitor)
        {
            var groupByWorkspace = from it in changeSet.Items
                                   let workspace = GetWorkspaceByLocalPath(it.LocalPath)
                                   group it by workspace into wg
                                   select wg;

            foreach (var workspace in groupByWorkspace)
            {
                var workspace1 = workspace;
                var changes = workspace.Key.PendingChanges.Where(pc => workspace1.Any(wi => string.Equals(pc.LocalItem, wi.LocalPath))).ToList();

                var result = TeamFoundationServerClient.Instance.CheckIn(workspace.Key, changes, changeSet.GlobalComment);

                if (result.Failures != null && result.Failures.Any(x => x.SeverityType == SeverityType.Error))
                {
                    MessageService.ShowError("Commit failed!", string.Join(Environment.NewLine, result.Failures.Select(f => f.Message)));
                    break;
                }
            }

            foreach (var file in changeSet.Items.Where(i => !i.IsDirectory))
            {
                FileService.NotifyFileChanged(file.LocalPath);
            }

            _cache.RefreshItems(changeSet.Items.Select(i => i.LocalPath));
        }

        protected override void OnDeleteDirectories(FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
        {
            foreach (var ws in GroupFilesPerWorkspace(localPaths))
            {
                List<Failure> failures;
                ws.Key.PendDelete(ws.ToList(), RecursionType.Full, keepLocal, out failures);
                if (failures.Any(f => f.SeverityType == SeverityType.Error))
                {
                    foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
                    {
                        monitor.ReportError(failure.Code, new Exception(failure.Message));
                    }
                }
            }

            _cache.RefreshItems(localPaths);
            FileService.NotifyFilesChanged(localPaths);
        }

        protected override void OnDeleteFiles(FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
        {
            foreach (var ws in GroupFilesPerWorkspace(localPaths))
            {
                List<Failure> failures;
                ws.Key.PendDelete(ws.ToList(), RecursionType.None, keepLocal, out failures);
                if (failures.Any(f => f.SeverityType == SeverityType.Error))
                {
                    foreach (var failure in failures.Where(f => f.SeverityType == SeverityType.Error))
                    {
                        monitor.ReportError(failure.Code, new Exception(failure.Message));
                    }
                }
            }

            _cache.RefreshItems(localPaths);
            FileService.NotifyFilesChanged(localPaths);
        }

        protected override VersionInfo[] OnGetDirectoryVersionInfo(FilePath localDirectory, bool getRemoteStatus, bool recursive)
        {
            var solutions = IdeApp.Workspace.GetAllSolutions().Where(s => s.BaseDirectory.IsChildPathOf(localDirectory) || s.BaseDirectory == localDirectory);
            List<FilePath> paths = new List<FilePath>();
            paths.Add(localDirectory);
           
            foreach (var solution in solutions)
            {
                var sfiles = solution.GetItemFiles(true);
                paths.AddRange(sfiles.Where(f => f != localDirectory));
            }

            RecursionType recursionType = recursive ? RecursionType.Full : RecursionType.OneLevel;
            return GetItemsVersionInfo(paths, getRemoteStatus, recursionType);
        }

        protected override Revision[] OnGetHistory(FilePath localFile, Revision since)
        {
            var workspace = GetWorkspaceByLocalPath(localFile);
            var serverPath = workspace.GetServerPathForLocalPath(localFile);
            ItemSpec spec = new ItemSpec(serverPath, RecursionType.None);
            ChangesetVersionSpec versionFrom = null;
           
            if (since != null)
                versionFrom = new ChangesetVersionSpec(((TeamFoundationServerRevision)since).Version);
            
            return VersionControlService.QueryHistory(spec, VersionSpec.Latest, versionFrom, null).Select(x => new TeamFoundationServerRevision(this, serverPath, x)).ToArray();
        }

        protected override RevisionPath[] OnGetRevisionChanges(Revision revision)
        {
            var tfsRevision = (TeamFoundationServerRevision)revision;

            var changeSet = VersionControlService.QueryChangeset(tfsRevision.Version, true);
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
                
                var workspace = GetWorkspaceByServerPath(changesByItem.Key.ServerItem);
                string path = workspace.GetLocalPathForServerPath(changesByItem.Key.ServerItem);
                path = string.IsNullOrEmpty(path) ? changesByItem.Key.ServerItem : path;
                revisionPaths.Add(new RevisionPath(path, action, action.ToString()));
            }

            return revisionPaths.ToArray();
        }

        protected override string OnGetTextAtRevision(FilePath repositoryPath, Revision revision)
        {
            var tfsRevision = (TeamFoundationServerRevision)revision;

            if (tfsRevision.Version == 0)
                return string.Empty;
            
            var workspace = GetWorkspaceByLocalPath(repositoryPath);
            var serverPath = workspace.GetServerPathForLocalPath(repositoryPath);
            var items = VersionControlService.QueryItems(new ItemSpec(serverPath, RecursionType.None),
                            new ChangesetVersionSpec(tfsRevision.Version), DeletedState.Any, ItemType.Any, true);

            if (items.Count == 0)
                return string.Empty;
            
            return workspace.GetItemContent(items[0]);
        }

        protected override IEnumerable<VersionInfo> OnGetVersionInfo(IEnumerable<FilePath> paths, bool getRemoteStatus)
        {
            return GetItemsVersionInfo(paths.ToList(), getRemoteStatus, RecursionType.None);
        }

        protected override void OnIgnore(FilePath[] localPath)
        {
            throw new NotImplementedException();
        }

        protected override Repository OnPublish(string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override void OnRevert(FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
        {
            foreach (var ws in GroupFilesPerWorkspace(localPaths))
            {
                var specs = ws.Select(x => new ItemSpec(x, recurse ? RecursionType.Full : RecursionType.None)).ToList();
                var operations = ws.Key.Undo(specs, monitor);
                _cache.RefreshItems(operations);
                FileService.NotifyFilesChanged(operations);
            }

            FileService.NotifyFilesRemoved(localPaths.Where(x => !FileHelper.HasFile(x)));
        }

        protected override void OnRevertRevision(FilePath localPath, Revision revision, ProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override void OnRevertToRevision(FilePath localPath, Revision revision, ProgressMonitor monitor)
        {
            var spec = new ItemSpec(localPath, localPath.IsDirectory ? RecursionType.Full : RecursionType.None);
            var rev = (TeamFoundationServerRevision)revision;
            var request = new GetRequest(spec, new ChangesetVersionSpec(rev.Version));
            var workspace = GetWorkspaceByLocalPath(localPath);
           
            if (workspace != null)
            {
                TeamFoundationServerClient.Instance.Get(workspace, request, GetOptions.None, monitor);
                _cache.RefreshItem(localPath);
            }
        }

        protected override void OnUnignore(FilePath[] localPath)
        {
            throw new NotImplementedException();
        }

        protected override void OnUpdate(FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
        {
            foreach (var workspace in GroupFilesPerWorkspace(localPaths))
            {
                var getRequests = workspace.Select(file => new GetRequest(file, recurse ? RecursionType.Full : RecursionType.None, VersionSpec.Latest)).ToList();
                TeamFoundationServerClient.Instance.Get(workspace.Key, getRequests, GetOptions.None, monitor);
            }

            _cache.RefreshItems(localPaths);
        }

        internal Workspace GetWorkspaceByLocalPath(FilePath path)
        {
            return _workspaces.SingleOrDefault(x => x.IsLocalPathMapped(path));
        }

        internal void CheckoutFile(FilePath path)
        {
            using (var progress = VersionControl.VersionControlService.GetProgressMonitor("CheckOut"))
            {
                progress.Log.WriteLine("Start check out item: " + path);
                var workspace = GetWorkspaceByLocalPath(path);

                TeamFoundationServerClient.Instance.Get(workspace, new GetRequest(path, RecursionType.None, VersionSpec.Latest), GetOptions.GetAll);
                var failures = workspace.PendEdit(new List<FilePath> { path }, RecursionType.None, TeamFoundationServerClient.Settings.CheckOutLockLevel);

                if (failures.Any())
                {
                    var failuresDialog = new FailuresDialog(failures);
                    failuresDialog.Show();
                }

                _cache.RefreshItem(path);
                FileService.NotifyFileChanged(path);
                progress.ReportSuccess("Finish check out.");
            }
        }

        internal void AttachWorkspace(Workspace workspace)
        {
            if (workspace == null)
                throw new ArgumentNullException("Workspace not found");

            if (_workspaces.Contains(workspace))
                return;

            workspace.RefreshPendingChanges();
            _workspaces.Add(workspace);
        }

        internal void Refresh()
        {
            _cache.ClearCache();

            foreach (var workspace in _workspaces)
            {
                workspace.RefreshPendingChanges();
            }
        }

        internal List<Conflict> GetConflicts(List<FilePath> paths)
        {
            List<Conflict> conflicts = new List<Conflict>();
           
            foreach (var workspacePaths in GroupFilesPerWorkspace(paths))
            {
                conflicts.AddRange(workspacePaths.Key.GetConflicts(workspacePaths));
            }

            return conflicts;
        }

        List<IGrouping<Workspace, FilePath>> GroupFilesPerWorkspace(IEnumerable<FilePath> filePaths)
        {
            var filesPerWorkspace = from f in filePaths
                                    let workspace = GetWorkspaceByLocalPath(f)
                                    group f by workspace into wg
                                    select wg;
            
            return filesPerWorkspace.ToList();
        }

        VersionInfo[] GetItemsVersionInfo(List<FilePath> paths, bool getRemoteStatus, RecursionType recursive)
        {
            List<VersionInfo> infos = new List<VersionInfo>();
            var extendedItems = _cache.GetItems(paths, recursive);
           
            foreach (var item in extendedItems.Where(i => i.IsInWorkspace || (!i.IsInWorkspace && i.ChangeType.HasFlag(ChangeType.Delete))).Distinct())
            {
                infos.AddRange(GetItemVersionInfo(item, getRemoteStatus));
            }

            foreach (var path in paths)
            {
                var path1 = path;
                if (infos.All(i => path1.CanonicalPath != i.LocalPath.CanonicalPath))
                {
                    infos.Add(VersionInfo.CreateUnversioned(path1, FileHelper.HasFolder(path1)));
                }
            }

            return infos.ToArray();
        }

        IEnumerable<VersionInfo> GetItemVersionInfo(ExtendedItem item, bool getRemoteStatus)
        {
            var localStatus = GetLocalVersionStatus(item);
            var localRevision = GetLocalRevision(item);
            var remoteStatus = getRemoteStatus ? GetServerVersionStatus(item) : VersionStatus.Versioned;
            var remoteRevision = getRemoteStatus ? GetServerRevision(item) : (TeamFoundationServerRevision)null;
            var path = item.LocalItem;
           
            if (string.IsNullOrEmpty(path) && item.ChangeType.HasFlag(ChangeType.Delete))
            {
                var workspace = GetWorkspaceByServerPath(item.ServerPath);
                path = workspace.GetLocalPathForServerPath(item.ServerPath);
            }

            yield return new VersionInfo(path, item.ServerPath, item.ItemType == ItemType.Folder,
                localStatus, localRevision, remoteStatus, remoteRevision);
        }

        VersionStatus GetLocalVersionStatus(ExtendedItem item)
        {
            if (item == null)
                return VersionStatus.Unversioned;
           
            var workspace = GetWorkspaceByServerPath(item.ServerPath);
          
            if (workspace == null)
                return VersionStatus.Unversioned;
           
            var status = VersionStatus.Versioned;

            if (item.IsLocked) 
            {
                if (item.HasOtherPendingChange)
                    status |= VersionStatus.Locked;
                else
                    status |= VersionStatus.LockOwned; 
            }

            var changes = workspace.PendingChanges.Where(ch => string.Equals(ch.ServerItem, item.ServerPath, StringComparison.OrdinalIgnoreCase)).ToList();

            if (changes.Any(change => change.IsAdd || change.Version == 0))
            {
                status |= VersionStatus.ScheduledAdd;
                return status;
            }

            if (changes.Any(change => change.IsDelete))
            {
                status |= VersionStatus.ScheduledDelete;
                return status;
            }

            if (changes.Any(change => change.IsRename))
            {
                status = status | VersionStatus.ScheduledAdd;
                return status;
            }

            if (changes.Any(change => change.IsEdit || change.IsEncoding))
            {
                status = status | VersionStatus.Modified;
                return status;
            }

            return status;
        }

        Revision GetLocalRevision(ExtendedItem item)
        {
            return new TeamFoundationServerRevision(this, item.VersionLocal, item.SourceServerItem);
        }

        Revision GetServerRevision(ExtendedItem item)
        {
            return new TeamFoundationServerRevision(this, item.VersionLatest, item.SourceServerItem);
        }

        VersionStatus GetServerVersionStatus(ExtendedItem item)
        {
            if (item == null)
                return VersionStatus.Unversioned;
            
            var status = VersionStatus.Versioned;
          
            if (item.IsLocked)
                status = status | VersionStatus.Locked;
            
            if (item.DeletionId > 0)
                return status | VersionStatus.Missing;
            
            if (item.VersionLatest > item.VersionLocal)
                return status | VersionStatus.Modified;

            return status;
        }

        Workspace GetWorkspaceByServerPath(string path)
        {
            return _workspaces.SingleOrDefault(x => x.IsServerPathMapped(path));
        }
    }
}
