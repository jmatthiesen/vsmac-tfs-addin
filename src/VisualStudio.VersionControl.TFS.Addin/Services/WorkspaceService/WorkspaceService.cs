// Workspace.cs
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
using System.Text;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
    /// Workspace service.
    /// </summary>
    sealed class WorkspaceService : IWorkspaceService
    {
        readonly ProjectCollection collection;
        readonly TeamFoundationServerVersionControlService _versionControlService;
        readonly WorkspaceData workspaceData;
        readonly ILoggingService _loggingService;
        readonly IProgressService _progressService;

        public WorkspaceService(WorkspaceData data, ProjectCollection collection, TeamFoundationServerVersionControlService versionControlService,
            ILoggingService loggingService, IProgressService progressService)
        {
            if (data == null || collection == null)
                return;
            
            workspaceData = data;
            this.collection = collection;
            _versionControlService = versionControlService;
            _loggingService = loggingService;
            _progressService = progressService;
        }

        /// <summary>
        /// Check in.
        /// </summary>
        /// <returns>The in.</returns>
        /// <param name="changes">Changes.</param>
        /// <param name="comment">Comment.</param>
        /// <param name="workItems">Work items.</param>
        public CheckInResult CheckIn(ICollection<PendingChange> changes, string comment, Dictionary<int, WorkItemCheckinAction> workItems)
        {
            var commitItems = (from it in changes
                               let needUpload = it.LocalItem.IsFile && (it.IsAdd || it.IsEdit)
                               select new CommitItem
                               {
                                   LocalPath = it.LocalItem,
                                   NeedUpload = needUpload
                               }).ToArray();
        
            return CheckIn(commitItems, comment, workItems);
        }
        
        /// <summary>
        /// Check in.
        /// </summary>
        /// <returns>The in.</returns>
        /// <param name="changes">Changes.</param>
        /// <param name="comment">Comment.</param>
        /// <param name="workItems">Work items.</param>
        public CheckInResult CheckIn(CommitItem[] changes, string comment, Dictionary<int, WorkItemCheckinAction> workItems)
        {
            changes.RemoveAll(ch => !Data.IsLocalPathMapped(ch.LocalPath));
            changes.ForEach(ch => ch.RepositoryPath = Data.GetServerPathForLocalPath(ch.LocalPath));
          
            foreach (var commitItem in changes.Where(c => c.NeedUpload))
            {
                collection.UploadFile(workspaceData, commitItem);
            }
           
            var result = collection.CheckIn(workspaceData, changes.Select(c => c.RepositoryPath), comment, workItems);
           
            if (result.ChangeSet > 0 && workItems.Count > 0)
            {
                WorkItemManager wm = new WorkItemManager(collection);
                wm.UpdateWorkItems(result.ChangeSet, workItems, comment);
            }

            ProcessGetOperations(result.LocalVersionUpdates, ProcessType.Get);
         
            foreach (var path in changes.Select(c => c.LocalPath))
            {
                path.MakeReadOnly();
            }

            return result;
        }

        #region Pending Changes

        /// <summary>
        /// Gets the pending changes.
        /// </summary>
        /// <returns>The pending changes.</returns>
        /// <param name="items">Items.</param>
        public List<PendingChange> GetPendingChanges(IEnumerable<ItemSpec> items)
        {
            return collection.QueryPendingChangesForWorkspace(workspaceData, items, false);
        }

        /// <summary>
        /// Gets the pending changes.
        /// </summary>
        /// <returns>The pending changes.</returns>
        /// <param name="items">Items.</param>
        public List<PendingChange> GetPendingChanges(IEnumerable<BaseItem> items)
        {
            return collection.QueryPendingChangesForWorkspace(workspaceData, items.Select(ItemSpec.FromServerItem), false);
        }

        /// <summary>
        /// Gets the pending sets.
        /// </summary>
        /// <returns>The pending sets.</returns>
        /// <param name="item">Item.</param>
        /// <param name="recurse">Recurse.</param>
        public List<PendingSet> GetPendingSets(string item, RecursionType recurse)
        {
            ItemSpec[] items = { new ItemSpec(item, recurse) };
            return collection.QueryPendingSets(Data.Name, Data.Owner, string.Empty, string.Empty, items, false);
        }

        #endregion

        #region GetItems

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <returns>The item.</returns>
        /// <param name="item">Item.</param>
        /// <param name="itemType">Item type.</param>
        /// <param name="includeDownloadUrl">If set to <c>true</c> include download URL.</param>
        public Item GetItem(ItemSpec item, ItemType itemType, bool includeDownloadUrl)
        {
            var items = GetItems(item.ToEnumerable(), VersionSpec.Latest, DeletedState.Any, itemType, includeDownloadUrl);
            return items.SingleOrDefault();
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <returns>The items.</returns>
        /// <param name="itemSpecs">Item specs.</param>
        /// <param name="versionSpec">Version spec.</param>
        /// <param name="deletedState">Deleted state.</param>
        /// <param name="itemType">Item type.</param>
        /// <param name="includeDownloadUrl">If set to <c>true</c> include download URL.</param>
        public List<Item> GetItems(IEnumerable<ItemSpec> itemSpecs, VersionSpec versionSpec, DeletedState deletedState, ItemType itemType, bool includeDownloadUrl)
        {
            return collection.QueryItems(workspaceData, itemSpecs, versionSpec, deletedState, itemType, includeDownloadUrl);
        }

        /// <summary>
        /// Gets the extended item.
        /// </summary>
        /// <returns>The extended item.</returns>
        /// <param name="item">Item.</param>
        /// <param name="itemType">Item type.</param>
        public ExtendedItem GetExtendedItem(ItemSpec item, ItemType itemType)
        {
            var items = collection.QueryItemsExtended(workspaceData, new[] { item }, DeletedState.Any, itemType);
            return items.SingleOrDefault();
        }

        /// <summary>
        /// Gets the extended items.
        /// </summary>
        /// <returns>The extended items.</returns>
        /// <param name="itemSpecs">Item specs.</param>
        /// <param name="deletedState">Deleted state.</param>
        /// <param name="itemType">Item type.</param>
        public List<ExtendedItem> GetExtendedItems(IEnumerable<ItemSpec> itemSpecs, DeletedState deletedState, ItemType itemType)
        {
            return collection.QueryItemsExtended(workspaceData, itemSpecs, deletedState, itemType);
        }

        #endregion

        /// <summary>
        /// Map the specified serverPath and localPath.
        /// </summary>
        /// <param name="serverPath">Server path.</param>
        /// <param name="localPath">Local path.</param>
        public void Map(string serverPath, string localPath)
        {
            Data.WorkingFolders.Add(new WorkingFolder(serverPath, localPath));
            Update();
        }

        void Update()
        {
            collection.UpdateWorkspace(Data.Name, Data.Owner, workspaceData);
        }


        /// <summary>
        /// Resets the download status.
        /// </summary>
        /// <param name="itemId">Item identifier.</param>
        public void ResetDownloadStatus(int itemId)
        {
            var updateVer = new UpdateLocalVersion(itemId, LocalPath.Empty(), 0);
            var queue = new UpdateLocalVersionQueue(this);
            queue.QueueUpdate(updateVer);
            queue.Flush();
        }

        #region Version Control Operations
        
        /// <summary>
        /// Get.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="options">Options.</param>
        public void Get(GetRequest request, GetOptions options)
        {
            Get(request.ToEnumerable(), options);
        }

        /// <summary>
        /// Get.
        /// </summary>
        /// <param name="requests">Requests.</param>
        /// <param name="options">Options.</param>
        public void Get(IEnumerable<GetRequest> requests, GetOptions options)
        {
            bool force = options.HasFlag(GetOptions.GetAll);
            bool noGet = options.HasFlag(GetOptions.Preview);

            var getOperations = collection.Get(workspaceData, requests, force, noGet);   
            ProcessGetOperations(getOperations, ProcessType.Get);
        }

        void CollectPaths(LocalPath root, List<ChangeRequest> paths)
        {
            if (!root.IsDirectory)
                return;
            
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                paths.Add(new ChangeRequest((LocalPath)dir, RequestType.Add, ItemType.Folder));
                CollectPaths(dir, paths);
            }

            paths.AddRange(Directory.EnumerateFiles(root).Select(file => new ChangeRequest((LocalPath) file, RequestType.Add, ItemType.File)));
        }

        /// <summary>
        /// Pend add.
        /// </summary>
        /// <param name="paths">Paths.</param>
        /// <param name="isRecursive">If set to <c>true</c> is recursive.</param>
        /// <param name="failures">Failures.</param>
        public void PendAdd(IEnumerable<LocalPath> paths, bool isRecursive, out ICollection<Failure> failures)
        {
            List<ChangeRequest> changes = new List<ChangeRequest>();

            foreach (var path in paths)
            {
                var itemType = path.IsDirectory ? ItemType.Folder : ItemType.File;
                changes.Add(new ChangeRequest(path, RequestType.Add, itemType));
               
                if (isRecursive && itemType == ItemType.Folder)
                {
                    CollectPaths(path, changes);
                }
            }

            if (changes.Count == 0)
            {
                failures = new List<Failure>();
                return;
            }

            var operations = collection.PendChanges(workspaceData, changes, out failures);
            ProcessGetOperations(operations, ProcessType.Add);
        }

        // Delete from Version Control, but don't delete file from file system - Monodevelop Logic.
        public void PendDelete(IEnumerable<LocalPath> paths, RecursionType recursionType, bool keepLocal, out ICollection<Failure> failures)
        {
            var changes = paths.Select(p => new ChangeRequest(p, RequestType.Delete, Directory.Exists(p) ? ItemType.Folder : ItemType.File, recursionType, LockLevel.None, VersionSpec.Latest)).ToList();

            if (changes.Count == 0)
            {
                failures = new List<Failure>();
                return;
            }

            var getOperations = collection.PendChanges(workspaceData, changes, out failures);
            var processType = keepLocal ? ProcessType.DeleteKeep : ProcessType.Delete;
            ProcessGetOperations(getOperations, processType);
        }

        /// <summary>
        /// Pend edit.
        /// </summary>
        /// <param name="paths">Paths.</param>
        /// <param name="recursionType">Recursion type.</param>
        /// <param name="lockLevel">Lock level.</param>
        /// <param name="failures">Failures.</param>
        public void PendEdit(IEnumerable<BasePath> paths, RecursionType recursionType, LockLevel lockLevel, out ICollection<Failure> failures)
        {
            var changes = paths.Select(p => new ChangeRequest(p, RequestType.Edit, ItemType.File, recursionType, lockLevel, VersionSpec.Latest)).ToList();
            if (changes.Count == 0)
            {
                failures = new List<Failure>();
                return;
            }

            var getOperations = collection.PendChanges(workspaceData, changes, out failures);
            ProcessGetOperations(getOperations, ProcessType.Edit);
        }

        /// <summary>
        /// Pend rename.
        /// </summary>
        /// <param name="oldPath">Old path.</param>
        /// <param name="newPath">New path.</param>
        /// <param name="failures">Failures.</param>
        public void PendRename(LocalPath oldPath, LocalPath newPath, out ICollection<Failure> failures)
        {
            List<ChangeRequest> changes = new List<ChangeRequest>
            {
                new ChangeRequest(oldPath, newPath, RequestType.Rename, oldPath.IsDirectory ? ItemType.Folder : ItemType.File)
            };
            var getOperations = collection.PendChanges(workspaceData, changes, out failures);
            ProcessGetOperations(getOperations, ProcessType.Rename);
        }

        public List<LocalPath> Undo(IEnumerable<ItemSpec> items)
        {
            var operations = collection.UndoPendChanges(workspaceData, items);
            UndoGetOperations(operations);
            return operations.Select(op => op.TargetLocalItem).ToList();
        }

        public void UnLockItems(IEnumerable<BasePath> paths)
        {
            LockItems(paths, LockLevel.None);
        }

        /// <summary>
        /// Locks items.
        /// </summary>
        /// <param name="paths">Paths.</param>
        /// <param name="lockLevel">Lock level.</param>
        public void LockItems(IEnumerable<BasePath> paths, LockLevel lockLevel)
        {
            var changes = (from p in paths
                let path = p is LocalPath ? Data.GetServerPathForLocalPath((LocalPath) p) : (RepositoryPath) p
                let recur = p.IsDirectory ? RecursionType.Full : RecursionType.None
                let itemType = p.IsDirectory ? ItemType.Folder : ItemType.File
                select new ChangeRequest(path, RequestType.Lock, itemType, recur, lockLevel, VersionSpec.Latest)).ToArray();

            if (changes.Length == 0)
                return;

			var getOperations = collection.PendChanges(workspaceData, changes, out ICollection<Failure> failures);
			ProcessGetOperations(getOperations, ProcessType.Get);
        }

        public List<Changeset> QueryHistory(ItemSpec item, VersionSpec versionItem, VersionSpec versionFrom, VersionSpec versionTo, short maxCount)
        {
            return collection.QueryHistory(item, versionItem, versionFrom, versionTo, maxCount);
        }

        public Changeset QueryChangeset(int changeSetId, bool includeChanges = false, bool includeDownloadUrls = false, bool includeSourceRenames = true)
        {
            return collection.QueryChangeset(changeSetId, includeChanges, includeDownloadUrls, includeSourceRenames);
        }

        public List<Conflict> GetConflicts(IEnumerable<LocalPath> paths)
        {
            var itemSpecs = paths.Select(p => new ItemSpec(p, RecursionType.Full)).ToList();
           
            return collection.QueryConflicts(workspaceData, itemSpecs);
        }

        public void Resolve(Conflict conflict, ResolutionType resolutionType)
        {
            var result = collection.Resolve(workspaceData, conflict, resolutionType);
            ProcessGetOperations(result.GetOperations, ProcessType.Get);
            Undo(result.UndoOperations.Select(x => new ItemSpec(x.TargetLocalItem, RecursionType.None)));
        }

        public LocalPath DownloadToTempWithName(string downloadUrl, string fileName)
        {
            return collection.DownloadToTempWithName(downloadUrl, fileName);
        }

        public LocalPath DownloadToTemp(string downloadUrl)
        {
            return collection.DownloadToTemp(downloadUrl);
        }

        public void UpdateLocalVersion(UpdateLocalVersionQueue updateLocalVersionQueue)
        {
            collection.UpdateLocalVersion(workspaceData, updateLocalVersionQueue);
        }

        public void CheckOut(IEnumerable<LocalPath> paths, out ICollection<Failure> failures)
        {
            foreach (var localPath in paths)
            {
                Get(new GetRequest(localPath, RecursionType.None, VersionSpec.Latest), GetOptions.GetAll);
                PendEdit(new[] { localPath }, RecursionType.None, _versionControlService.CheckOutLockLevel, out failures);
              
                if (failures.Any())
                    return;
            }

            failures = new List<Failure>();
        }


        #endregion

        internal void UnsetDirectoryAttributes(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] localFiles = dir.GetFiles("*", SearchOption.AllDirectories);
       
            foreach (FileInfo file in localFiles)
                File.SetAttributes(file.FullName, FileAttributes.Normal);
        }

        #region Equal

        #region IComparable<Workspace> Members

        public int CompareTo(IWorkspaceService other)
        {
            var nameCompare = string.Compare(Data.Name, other.Data.Name, StringComparison.Ordinal);
         
            if (nameCompare != 0)
                return nameCompare;
         
            return string.Compare(Data.Owner, other.Data.Owner, StringComparison.Ordinal);
        }

        #endregion

        #region IEquatable<Workspace> Members

        public bool Equals(IWorkspaceService other)
        {
            if (ReferenceEquals(null, other))
                return false;
            
            if (ReferenceEquals(this, other))
                return true;
            
            return string.Equals(other.Data.Name, Data.Name) && string.Equals(other.Data.Owner, Data.Owner);
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            
            if (ReferenceEquals(this, obj))
                return true;
            
            var cast = obj as IWorkspaceService;

            if (cast == null)
                return false;
            
            return Equals(cast);
        }

        public override int GetHashCode()
        {
            var hash = Data.Name.GetHashCode();
            hash ^= 307 * Data.Owner.GetHashCode();
            return hash;
        }

        public static bool operator ==(WorkspaceService left, WorkspaceService right)
        {
            return ReferenceEquals(null, left) ? ReferenceEquals(null, right) : left.Equals(right);
        }

        public static bool operator !=(WorkspaceService left, WorkspaceService right)
        {
            return !(left == right);
        }

        #endregion Equal

        #region Process Get Operations

        /// <summary>
        /// Download file.
        /// </summary>
        /// <returns>The file.</returns>
        /// <param name="operation">Operation.</param>
        LocalPath DownloadFile(GetOperation operation)
        {
            var path = operation.TargetLocalItem.IsEmpty ? operation.SourceLocalItem : operation.TargetLocalItem;
         
            if (path.IsEmpty)
                return LocalPath.Empty();
            
            if (operation.ItemType == ItemType.Folder)
            {
                if (!path.Exists)
                    Directory.CreateDirectory(path);
              
                return path;
            }

            if (operation.ItemType == ItemType.File)
            {
                if (!path.GetDirectory().Exists)
                    Directory.CreateDirectory(path.GetDirectory());
              
                return collection.Download(path, operation.ArtifactUri);
            }

            return LocalPath.Empty();
        }

        UpdateLocalVersion ProcessAdd(GetOperation operation, ProcessDirection processDirection)
        {
            if (processDirection == ProcessDirection.Undo)
            {
                operation.TargetLocalItem.Delete();
            }

            return null;
        }

        UpdateLocalVersion ProcessEdit(GetOperation operation, ProcessDirection processDirection)
        {
            if (processDirection == ProcessDirection.Undo)
            {
                LocalPath path = DownloadFile(operation);
                if (operation.ItemType == ItemType.File)
                    path.MakeReadOnly();
            }
            else
            {
                var path = string.IsNullOrEmpty(operation.TargetLocalItem) ? operation.SourceLocalItem : operation.TargetLocalItem;
                path.MakeWritable();
            }

            return new UpdateLocalVersion(operation.ItemId, operation.TargetLocalItem, operation.VersionServer);
        }

        UpdateLocalVersion ProcessGet(GetOperation operation, ProcessDirection processDirection)
        {
            if (processDirection == ProcessDirection.Normal)
            {
                LocalPath path = DownloadFile(operation);
             
                if (operation.ItemType == ItemType.File)
                    path.MakeReadOnly();
                
                return new UpdateLocalVersion(operation.ItemId, path, operation.VersionServer);
            }

            return null;
        }

        UpdateLocalVersion ProcessDelete(GetOperation operation, ProcessDirection processDirection, ProcessType processType)
        {
            if (processDirection == ProcessDirection.Undo)
            {
                var update = ProcessGet(operation, ProcessDirection.Normal);
                return update;
            }

            return InternalProcessDelete(operation, processType);
        }

        UpdateLocalVersion InternalProcessDelete(GetOperation operation, ProcessType processType)
        {
            var path = operation.SourceLocalItem;
          
            if (processType == ProcessType.Delete)
            {
                try
                {
                    path.Delete();
                }
                catch
                {
                    _loggingService.LogToInfo("Can not delete path:" + path);
                }
            }

            return new UpdateLocalVersion(operation.ItemId, LocalPath.Empty(), operation.VersionServer);
        }

        /// <summary>
        /// Processes the rename operation.
        /// </summary>
        /// <returns>The rename.</returns>
        /// <param name="operation">Operation.</param>
        UpdateLocalVersion ProcessRename(GetOperation operation)
        {
            //If the operation is called by Repository OnMoveFile or OnMoveDirectory file/folder is moved before this method.
            //When is called by Source Exporer or By Revert command file is not moved
            bool hasBeenMoved = !operation.SourceLocalItem.Exists && operation.TargetLocalItem.Exists;
           
            if (!hasBeenMoved)
            {
                operation.SourceLocalItem.MoveTo(operation.TargetLocalItem);
            }

            return new UpdateLocalVersion(operation.ItemId, operation.TargetLocalItem, operation.VersionServer);
        }

        enum ProcessDirection
        {
            Normal,
            Undo
        }

        enum ProcessType
        {
            Get,
            Add,
            Edit,
            Rename,
            Delete,
            DeleteKeep
        }

        /// <summary>
        /// Processes the get operations.
        /// </summary>
        /// <param name="getOperations">Get operations.</param>
        /// <param name="processType">Process type.</param>
        void ProcessGetOperations(IReadOnlyCollection<GetOperation> getOperations, ProcessType processType)
        {
            if (getOperations == null || getOperations.Count == 0)
                return;
            
            using (var progressDisplay = _progressService.CreateProgress())
            {
                progressDisplay.BeginTask("Process", getOperations.Count);
                UpdateLocalVersionQueue updates = new UpdateLocalVersionQueue(this);
                foreach (var operation in getOperations)
                {
                    try
                    {
                        if (progressDisplay.IsCancelRequested)
                            break;
                        
                        progressDisplay.BeginTask(processType + " " + operation.TargetLocalItem, 1);
                        UpdateLocalVersion update;

                        switch (processType)
                        {
                            case ProcessType.Add:
                                update = ProcessAdd(operation, ProcessDirection.Normal);
                                break;
                            case ProcessType.Edit:
                                update = ProcessEdit(operation, ProcessDirection.Normal);
                                break;
                            case ProcessType.Get:
                                update = ProcessGet(operation, ProcessDirection.Normal);
                                break;
                            case ProcessType.Rename:
                                update = ProcessRename(operation);
                                break;
                            case ProcessType.Delete:
                            case ProcessType.DeleteKeep:
                                update = ProcessDelete(operation, ProcessDirection.Normal, processType);
                                break;
                            default:
                                update = ProcessGet(operation, ProcessDirection.Normal);
                                break;
                        }

                        if (update != null)
                            updates.QueueUpdate(update);
                    }
                    finally
                    {
                        progressDisplay.EndTask();
                    }
                }

                updates.Flush();
                progressDisplay.EndTask();
            }
        }

        void UndoGetOperations(List<GetOperation> getOperations)
        {
            if (getOperations == null || getOperations.Count == 0)
                return;
            
            using (var progressDisplay = _progressService.CreateProgress())
            {
                progressDisplay.BeginTask("Undo", getOperations.Count);
                UpdateLocalVersionQueue updates = new UpdateLocalVersionQueue(this);
                foreach (var operation in getOperations)
                {
                    try
                    {
                        if (progressDisplay.IsCancelRequested)
                            break;
                        string stepName = operation.ChangeType == ChangeType.None ? "Undo " : operation.ChangeType.ToString();
                        progressDisplay.BeginTask(stepName + " " + operation.TargetLocalItem, 1);
                        UpdateLocalVersion update;

                        if (operation.IsAdd)
                        {
                            update = ProcessAdd(operation, ProcessDirection.Undo);
                            if (update != null)
                                updates.QueueUpdate(update);
                            continue;
                        }

                        if (operation.IsDelete)
                        {
                            update = ProcessDelete(operation, ProcessDirection.Undo, ProcessType.Delete);
                            if (update != null)
                                updates.QueueUpdate(update);
                        }

                        if (operation.IsRename)
                        {
                            update = ProcessRename(operation);
                            if (update != null)
                                updates.QueueUpdate(update);
                        }
                        if (operation.IsEdit || operation.IsEncoding)
                        {
                            update = ProcessEdit(operation, ProcessDirection.Undo);
                            if (update != null)
                                updates.QueueUpdate(update);
                        }
                    }
                    finally
                    {
                        progressDisplay.EndTask();
                    }
                }

                updates.Flush();
                progressDisplay.EndTask();
            }
        }

        #endregion

        public string GetItemContent(Item item)
        {
            if (item == null || item.ItemType == ItemType.Folder)
                return string.Empty;
            
            if (item.DeletionId > 0)
                return string.Empty;
            
            var tempName = collection.DownloadToTemp(item.ArtifactUri);
            var text = item.Encoding > 0 ? File.ReadAllText(tempName, Encoding.GetEncoding(item.Encoding)) :
                       File.ReadAllText(tempName);
            tempName.Delete();

            return text;
        }

        public WorkspaceData Data { get { return workspaceData; }}

        public ProjectCollection ProjectCollection { get { return collection; } }

        public override string ToString()
        {
            return "Owner: " + Data.Owner + ", Name: " + Data.Name;
        }
    }
}