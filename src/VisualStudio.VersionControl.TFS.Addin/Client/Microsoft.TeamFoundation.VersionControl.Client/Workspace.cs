//
// Microsoft.TeamFoundation.VersionControl.Client.Workspace
//
// Authors:
//	Joel Reed (joelwreed@gmail.com)
//  Ventsislav Mladenov (ventsislav.mladenov@gmail.com)
//  Javier Suárez Ruiz (javiersuarezruiz@hotmail.com)
//
// Copyright (C) 2018 Joel Reed, Ventsislav Mladenov, Javier Suárez Ruiz
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using MonoDevelop.Core;
using Microsoft.TeamFoundation.VersionControl.Client.Objects;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using MonoDevelop.Ide;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client.Enums;
using MonoDevelop.Projects;
using MonoDevelop.Ide.ProgressMonitoring;

namespace Microsoft.TeamFoundation.VersionControl.Client
{
    public sealed class Workspace : IEquatable<Workspace>, IComparable<Workspace>
    {
        #region Constructors

        Workspace(string name, 
                  string ownerName, string comment, 
                  List<WorkingFolder> folders, string computer)
        {
            Name = name;
            OwnerName = ownerName;
            Comment = comment;
            Folders = folders;
            Computer = computer;
            PendingChanges = new List<PendingChange>();
        }

        public Workspace(RepositoryService versionControl, string name, 
                         string ownerName, string comment, 
                         List<WorkingFolder>  folders, string computer) 
            : this(name, ownerName, comment, folders, computer)
        {
            ProjectCollection = versionControl.Collection;
            VersionControlService = versionControl;
        }

        public Workspace(TeamFoundation.Client.ProjectCollection collection, string name, 
                         string ownerName, string comment, 
                         List<WorkingFolder>  folders, string computer)
            : this(name, ownerName, comment, folders, computer)
        {
            ProjectCollection = collection;
            VersionControlService = collection.GetService<RepositoryService>();
        }

        public Workspace(RepositoryService versionControl, WorkspaceData workspaceData) 
            : this(versionControl, workspaceData.Name, workspaceData.Owner, workspaceData.Comment, workspaceData.WorkingFolders, workspaceData.Computer)
        {
        }

        public Workspace(TeamFoundation.Client.ProjectCollection collection, WorkspaceData workspaceData) 
            : this(collection, workspaceData.Name, workspaceData.Owner, workspaceData.Comment, workspaceData.WorkingFolders, workspaceData.Computer)
        {
        }

        #endregion

        public CheckInResult CheckIn(List<PendingChange> changes, string comment, Dictionary<int, WorkItemCheckinAction> workItems)
        {
            foreach (var change in changes)
            {
                VersionControlService.UploadFile(this, change);
            }

            var result = VersionControlService.CheckIn(this, changes, comment, workItems);
          
            if (result.ChangeSet > 0)
            {
                WorkItemManager wm = new WorkItemManager(this.ProjectCollection);
                wm.UpdateWorkItems(result.ChangeSet, workItems, comment);
            }

            RefreshPendingChanges();
            ProcessGetOperations(result.LocalVersionUpdates, ProcessType.Get);
          
            foreach (var file in changes.Where(ch => ch.ItemType == ItemType.File && !string.IsNullOrEmpty(ch.LocalItem)).Select(ch => ch.LocalItem).Distinct())
            {
                MakeFileReadOnly(file);
            }

            return result;
        }

        #region Pending Changes

        public List<PendingChange> PendingChanges { get; set; }

        public void RefreshPendingChanges()
        {
            PendingChanges.Clear();
            var paths = Folders.Select(f => f.LocalItem).ToArray();
            PendingChanges.AddRange(GetPendingChanges(paths, RecursionType.Full));
        }

        public List<PendingChange> GetPendingChanges()
        {
            return GetPendingChanges(VersionControlPath.RootFolder, RecursionType.Full);
        }

        public List<PendingChange> GetPendingChanges(string item)
        {
            return GetPendingChanges(item, RecursionType.None);
        }

        public List<PendingChange> GetPendingChanges(string item, RecursionType rtype)
        {
            return GetPendingChanges(item, rtype, false);
        }

        public List<PendingChange> GetPendingChanges(string item, RecursionType rtype,
                                                     bool includeDownloadInfo)
        {
            string[] items = { item };
            return GetPendingChanges(items, rtype, includeDownloadInfo);
        }

        public List<PendingChange> GetPendingChanges(string[] items, RecursionType rtype)
        {
            return GetPendingChanges(items, rtype, false);
        }

        public List<PendingChange> GetPendingChanges(string[] items, RecursionType rtype,
                                                     bool includeDownloadInfo)
        {

            var itemSpecs = new List<ItemSpec>(items.Select(i => new ItemSpec(i, rtype)));
            return VersionControlService.QueryPendingChangesForWorkspace(this, itemSpecs, includeDownloadInfo);
        }

        public List<PendingChange> GetPendingChanges(List<ItemSpec> items)
        {
            return VersionControlService.QueryPendingChangesForWorkspace(this, items, false);
        }

        public List<PendingSet> GetPendingSets(string item, RecursionType recurse)
        {
            ItemSpec[] items = { new ItemSpec(item, recurse) };
            return VersionControlService.QueryPendingSets(Name, OwnerName, string.Empty, string.Empty, items, false);
        }

        #endregion

        #region GetItems

        public Item GetItem(string path, ItemType itemType)
        {
            return GetItem(path, itemType, false);
        }

        public Item GetItem(string path, ItemType itemType, bool includeDownloadUrl)
        {
            var itemSpec = new ItemSpec(path, RecursionType.None);
            var items = VersionControlService.QueryItems(this, itemSpec, VersionSpec.Latest, DeletedState.Any, itemType, includeDownloadUrl);
            return items.SingleOrDefault();
        }

        public ExtendedItem GetExtendedItem(string path, ItemType itemType)
        {
            var itemSpec = new ItemSpec(path, RecursionType.None);
            var items = VersionControlService.QueryItemsExtended(this, itemSpec, DeletedState.Any, itemType);
            return items.SingleOrDefault();
        }

        public List<ExtendedItem> GetExtendedItems(List<ItemSpec> itemSpecs,
                                                   DeletedState deletedState,
                                                   ItemType itemType)
        {
            return this.VersionControlService.QueryItemsExtended(this.Name, this.OwnerName, itemSpecs, deletedState, itemType);
        }

        #endregion

        public bool IsLocalPathMapped(string localPath)
        {
            if (localPath == null)
                throw new ArgumentNullException("localPath");
            
            return Folders.Any(f => localPath.StartsWith(f.LocalItem, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsServerPathMapped(VersionControlPath serverPath)
        {
            return Folders.Any(f => serverPath.IsChildOrEqualTo(f.ServerItem));
        }

        public VersionControlPath GetServerPathForLocalPath(string localItem)
        {
            var mappedFolder = Folders.FirstOrDefault(f => localItem.StartsWith(f.LocalItem, StringComparison.OrdinalIgnoreCase));
            if (mappedFolder == null)
                return null;
            if (string.Equals(mappedFolder.LocalItem, localItem, StringComparison.OrdinalIgnoreCase))
                return mappedFolder.ServerItem;
            else
            {
                string rest = TfsPath.LocalToServerPath(localItem.Substring(mappedFolder.LocalItem.Length));
                if (mappedFolder.ServerItem == VersionControlPath.RootFolder)
                    return "$" + rest;
                else
                    return mappedFolder.ServerItem + rest;
            }
        }

        public string GetLocalPathForServerPath(VersionControlPath serverItem)
        {
            var mappedFolder = Folders.FirstOrDefault(f => serverItem.IsChildOrEqualTo(f.ServerItem));
            if (mappedFolder == null)
                return null;
            if (serverItem == mappedFolder.ServerItem)
                return mappedFolder.LocalItem;
            else
            {
                //string rest = TfsPath.ServerToLocalPath(serverItem.ToString().Substring(mappedFolder.ServerItem.ToString().Length + 1));
                string rest = TfsPath.ServerToLocalPath(serverItem.ChildPart(mappedFolder.ServerItem));
                return Path.Combine(mappedFolder.LocalItem, rest);
            }
        }

        public WorkingFolder GetWorkingFolderForServerItem(string serverItem)
        {
            int maxPath = 0;
            WorkingFolder workingFolder = null;

            foreach (WorkingFolder folder in Folders)
            {
                if (!serverItem.StartsWith(folder.ServerItem, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (folder.LocalItem.Length > maxPath)
                {
                    workingFolder = folder;
                    maxPath = folder.LocalItem.Length;
                }
            }

            return workingFolder;
        }

        public void Map(string serverPath, string localPath)
        {
            Folders.Add(new WorkingFolder(serverPath, localPath));
            Update();
        }

        void Update()
        {
            VersionControlService.UpdateWorkspace(this.Name, this.OwnerName, this);
        }


        public void ResetDownloadStatus(int itemId)
        {
            var updateVer = new UpdateLocalVersion(itemId, string.Empty, 0);
            var queue = new UpdateLocalVersionQueue(this);
            queue.QueueUpdate(updateVer);
            queue.Flush();
        }

        #region Version Control Operations

        public GetStatus Get(GetRequest request, GetOptions options, MessageDialogProgressMonitor monitor = null)
        {
            var requests = new List<GetRequest> { request };
            return Get(requests, options, monitor);
        }

        public GetStatus Get(List<GetRequest> requests, GetOptions options, MessageDialogProgressMonitor monitor = null)
        {
            bool force = options.HasFlag(GetOptions.GetAll);
            bool noGet = options.HasFlag(GetOptions.Preview);

            var getOperations = VersionControlService.Get(this, requests, force, noGet);   
            ProcessGetOperations(getOperations, ProcessType.Get, monitor);
     
            return new GetStatus(getOperations.Count);
        }

        void CollectPaths(FilePath root, List<ChangeRequest> paths)
        {
            if (!root.IsDirectory)
                return;
            
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                paths.Add(new ChangeRequest(dir, RequestType.Add, ItemType.Folder));
                CollectPaths(dir, paths);
            }

            foreach (var file in Directory.EnumerateFiles(root))
            {
                paths.Add(new ChangeRequest(file, RequestType.Add, ItemType.File));
            }
        }

        public int PendAdd(List<FilePath> paths, bool isRecursive)
        {
            if (paths.Count == 0)
                return 0;

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

            List<Failure> failures;
            var operations = VersionControlService.PendChanges(this, changes, out failures);
            ProcessGetOperations(operations, ProcessType.Add);
            RefreshPendingChanges();
            return operations.Count;
        }

        //Delete from Version Control, but don't delete file from file system - Monodevelop Logic.
        public void PendDelete(List<FilePath> paths, RecursionType recursionType, bool keepLocal, out List<Failure> failures)
        {
            if (paths.Count == 0)
            {
                failures = new List<Failure>();
                return;
            }

            var changes = paths.Select(p => new ChangeRequest(p, RequestType.Delete, Directory.Exists(p) ? ItemType.Folder : ItemType.File, recursionType, LockLevel.None, VersionSpec.Latest)).ToList();
            var getOperations = VersionControlService.PendChanges(this, changes, out failures);
            var processType = keepLocal ? ProcessType.DeleteKeep : ProcessType.Delete;
            ProcessGetOperations(getOperations, processType);
            RefreshPendingChanges();
        }

        public List<Failure> PendEdit(FilePath path, RecursionType recursionType, CheckOutLockLevel checkOutlockLevel)
        {
            return PendEdit(new List<FilePath> { path }, recursionType, checkOutlockLevel);
        }

        public List<Failure> PendEdit(List<FilePath> paths, RecursionType recursionType, CheckOutLockLevel checkOutlockLevel)
        {
            if (paths.Count == 0)
                return new List<Failure>();
            LockLevel lockLevel = LockLevel.None;
            if (checkOutlockLevel == CheckOutLockLevel.CheckOut)
                lockLevel = LockLevel.CheckOut;
            else if (checkOutlockLevel == CheckOutLockLevel.CheckIn)
                lockLevel = LockLevel.Checkin;
            var changes = paths.Select(p => new ChangeRequest(p, RequestType.Edit, ItemType.File, recursionType, lockLevel, VersionSpec.Latest)).ToList();
            List<Failure> failures;
            var getOperations = VersionControlService.PendChanges(this, changes, out failures);
            ProcessGetOperations(getOperations, ProcessType.Edit);
            RefreshPendingChanges();

            return failures;
        }

        void PendRename(string oldPath, string newPath, ItemType itemType, out List<Failure> failures)
        {
            List<ChangeRequest> changes = new List<ChangeRequest>();
            changes.Add(new ChangeRequest(oldPath, newPath, RequestType.Rename, itemType));
            var getOperations = VersionControlService.PendChanges(this, changes, out failures);
            ProcessGetOperations(getOperations, ProcessType.Rename);
            RefreshPendingChanges();
        }

        public void PendRenameFile(string oldPath, string newPath, out List<Failure> failures)
        {
            PendRename(oldPath, newPath, ItemType.File, out failures);
        }

        public void PendRenameFolder(string oldPath, string newPath, out List<Failure> failures)
        {
            PendRename(oldPath, newPath, ItemType.Folder, out failures);
        }

        public List<FilePath> Undo(List<ItemSpec> items)
        {
            var operations = VersionControlService.UndoPendChanges(this, items);
            UndoGetOperations(operations);
            RefreshPendingChanges();
            List<FilePath> undoPaths = new List<FilePath>();
        
            foreach (var oper in operations)
            {
                undoPaths.Add(oper.TargetLocalItem);
            }

            return undoPaths;
        }

        public void LockFiles(List<string> paths, LockLevel lockLevel)
        {
            SetLock(paths, ItemType.File, lockLevel, RecursionType.None);
        }

        public void LockFolders(List<string> paths, LockLevel lockLevel)
        {
            SetLock(paths, ItemType.File, lockLevel, RecursionType.Full);
        }

        void SetLock(List<string> paths, ItemType itemType, LockLevel lockLevel, RecursionType recursion)
        {
            if (paths.Count == 0)
                return;

            var changes = paths.Select(p => new ChangeRequest(p, RequestType.Lock, itemType, recursion, lockLevel, VersionSpec.Latest)).ToList();
            List<Failure> failures;
            var getOperations = VersionControlService.PendChanges(this, changes, out failures);
            ProcessGetOperations(getOperations, ProcessType.Get);
            RefreshPendingChanges();
        }

        public List<Conflict> GetConflicts(IEnumerable<FilePath> paths)
        {
            var itemSpecs = paths.Select(p => new ItemSpec(p, RecursionType.Full)).ToList();
            return VersionControlService.QueryConflicts(this, itemSpecs);
        }

        public void Resolve(Conflict conflict, ResolutionType resolutionType)
        {
            var result = VersionControlService.Resolve(conflict, resolutionType);
            ProcessGetOperations(result.GetOperations, ProcessType.Get);
            Undo(result.UndoOperations.Select(x => new ItemSpec(x.TargetLocalItem, RecursionType.None)).ToList());
        }

        #endregion

        #region Serialization

        internal static Workspace FromXml(RepositoryService versionControl, XElement element)
        {
            string computer = element.Attribute("computer").Value;
            string name = element.Attribute("name").Value;
            string owner = element.Attribute("owner").Value;
            //bool isLocal = Convert.ToBoolean(element.Attribute("islocal").Value);

            string comment = element.Element(XmlNamespaces.GetMessageElementName("Comment")).Value;
            DateTime lastAccessDate = DateTime.Parse(element.Element(XmlNamespaces.GetMessageElementName("LastAccessDate")).Value);
            var folders = new List<WorkingFolder>(element.Element(XmlNamespaces.GetMessageElementName("Folders"))
                                                         .Elements(XmlNamespaces.GetMessageElementName("WorkingFolder"))
                                                         .Select(el => WorkingFolder.FromXml(el)));

            return new Workspace(versionControl, name, owner, comment, folders, computer)
            { 
                LastAccessDate = lastAccessDate 
            };
        }

        internal XElement ToXml(XName elementName)
        {
            var ns = elementName.Namespace;
            XElement element = new XElement(elementName, 
                                   new XAttribute("computer", Computer), 
                                   new XAttribute("name", Name),
                                   new XAttribute("owner", OwnerName), 
                                   new XElement(ns + "Comment", Comment));

            if (Folders != null)
            {
                element.Add(new XElement(ns + "Folders", Folders.Select(f => f.ToXml(ns))));
            }
            return element;
        }

        #endregion

        internal void MakeFileReadOnly(string path)
        {
            if (File.Exists(path))
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
        }

        internal void MakeFileWritable(string path)
        {
            if (File.Exists(path))
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
        }

        internal void UnsetDirectoryAttributes(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] localFiles = dir.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo file in localFiles)
                File.SetAttributes(file.FullName, FileAttributes.Normal);
        }

        #region Equal

        #region IComparable<Workspace> Members

        public int CompareTo(Workspace other)
        {
            var nameCompare = string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (nameCompare != 0)
                return nameCompare;
            return string.Compare(OwnerName, other.OwnerName, StringComparison.Ordinal);
        }

        #endregion

        #region IEquatable<Workspace> Members

        public bool Equals(Workspace other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(other.Name, Name) && string.Equals(other.OwnerName, OwnerName);
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            Workspace cast = obj as Workspace;
            if (cast == null)
                return false;
            return Equals(cast);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static bool operator ==(Workspace left, Workspace right)
        {
            return ReferenceEquals(null, left) ? ReferenceEquals(null, right) : left.Equals(right);
        }

        public static bool operator !=(Workspace left, Workspace right)
        {
            return !(left == right);
        }

        #endregion Equal

        #region Process Get Operations

        string DownloadFile(GetOperation operation, VersionControlDownloadService downloadService)
        {
            string path = string.IsNullOrEmpty(operation.TargetLocalItem) ? operation.SourceLocalItem : operation.TargetLocalItem;
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            if (operation.ItemType == ItemType.Folder)
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
            if (operation.ItemType == ItemType.File)
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                return downloadService.Download(path, operation.ArtifactUri);
            }
            return string.Empty;
        }

        UpdateLocalVersion ProcessAdd(GetOperation operation, ProcessDirection processDirection)
        {
            if (processDirection == ProcessDirection.Undo)
            {
                FileHelper.Delete(operation.ItemType, operation.TargetLocalItem);
            }
            return null;
        }

        UpdateLocalVersion ProcessEdit(GetOperation operation, VersionControlDownloadService downloadService, ProcessDirection processDirection)
        {
            if (processDirection == ProcessDirection.Undo)
            {
                var path = DownloadFile(operation, downloadService);
                if (operation.ItemType == ItemType.File)
                    MakeFileReadOnly(path);
            }
            else
            {
                string path = string.IsNullOrEmpty(operation.TargetLocalItem) ? operation.SourceLocalItem : operation.TargetLocalItem;
                MakeFileWritable(path);
            }
            return new UpdateLocalVersion(operation.ItemId, operation.TargetLocalItem, operation.VersionServer);
        }

        private UpdateLocalVersion ProcessGet(GetOperation operation, VersionControlDownloadService downloadService, ProcessDirection processDirection)
        {
            if (processDirection == ProcessDirection.Normal)
            {
                var path = DownloadFile(operation, downloadService);
                if (operation.ItemType == ItemType.File)
                    MakeFileReadOnly(path);
                return new UpdateLocalVersion(operation.ItemId, path, operation.VersionServer);
            }
            return null;
        }

        UpdateLocalVersion ProcessDelete(GetOperation operation, VersionControlDownloadService downloadService, ProcessDirection processDirection, ProcessType processType)
        {
            if (processDirection == ProcessDirection.Undo)
            {
                var update = ProcessGet(operation, downloadService, ProcessDirection.Normal);
                var filePath = (FilePath)operation.TargetLocalItem;
                var projects = IdeApp.Workspace.GetAllProjects();
                foreach (var project in projects)
                {
                    if (filePath.IsChildPathOf(project.BaseDirectory))
                    {
                        if (operation.ItemType == ItemType.File)
                            project.AddFile(operation.TargetLocalItem);
                        if (operation.ItemType == ItemType.Folder)
                            project.AddDirectory(operation.TargetLocalItem.Substring(((string)project.BaseDirectory).Length + 1));
                        break;
                    }
                }
                return update;
            }
            else
                return InternalProcessDelete(operation, processType);
        }

        UpdateLocalVersion InternalProcessDelete(GetOperation operation, ProcessType processType)
        {
            var path = operation.SourceLocalItem;
            if (processType == ProcessType.Delete)
            {
                try
                {
                    if (operation.ItemType == ItemType.File)
                    {
                        FileHelper.FileDelete(path);
                    }
                    else
                    {
                        FileHelper.FolderDelete(path);
                    }
                }
                catch
                {
                    LoggingService.Log(MonoDevelop.Core.Logging.LogLevel.Info, "Can not delete path:" + path);
                }
            }
            return new UpdateLocalVersion(operation.ItemId, null, operation.VersionServer);
        }

        void ProjectMoveFile(Project project, FilePath source, string destination)
        {
            foreach (var file in project.Files)
            {
                if (file.FilePath == source)
                {
                    project.Files.Remove(file);
                    break;
                }
            }
            project.AddFile(destination);
        }

        Project FindProjectContainingFolder(FilePath folder)
        {
            Project project = null;
            foreach (var prj in IdeApp.Workspace.GetAllProjects())
            {
                foreach (var file in prj.Files)
                {
                    if (file.Subtype == Subtype.Directory && file.FilePath == folder)
                    {
                        project = prj;
                        break;
                    }
                    if (file.Subtype == Subtype.Code && file.FilePath.IsChildPathOf(folder))
                    {
                        project = prj;
                        break;
                    }
                }
            }
            return project;
        }

        void ProjectMoveFolder(Project project, FilePath source, FilePath destination)
        {
            var filesToMove = new List<ProjectFile>();
            ProjectFile folderFile = null;
            foreach (var file in project.Files)
            {
                if (file.FilePath == source)
                {
                    folderFile = file;
                }
                if (file.FilePath.IsChildPathOf(source))
                {
                    filesToMove.Add(file);
                }
            }
            if (folderFile != null)
                project.Files.Remove(folderFile);

            var relativePath = destination.ToRelative(project.BaseDirectory);
            project.AddDirectory(relativePath);
            foreach (var file in filesToMove)
            {
                project.Files.Remove(file);
                var fileRelativePath = file.FilePath.ToRelative(source);
                var fileToAdd = Path.Combine(destination, fileRelativePath);
                if (FileHelper.HasFolder(fileToAdd))
                {
                    fileRelativePath = ((FilePath)fileToAdd).ToRelative(project.BaseDirectory);
                    project.AddDirectory(fileRelativePath);
                }
                else
                    project.AddFile(fileToAdd);
            }
        }

        UpdateLocalVersion ProcessRename(GetOperation operation, ProcessDirection processDirection)
        {
            //If the operation is called by Repository OnMoveFile or OnMoveDirectory file/folder is moved before this method.
            //When is called by Source Exporer or By Revert command file is not moved
            bool hasBeenMoved = !FileHelper.Exists(operation.SourceLocalItem) && FileHelper.Exists(operation.TargetLocalItem);
            if (!hasBeenMoved)
            {
                var found = false;
                if (operation.ItemType == ItemType.File)
                {
                    var project = IdeApp.Workspace.GetProjectsContainingFile(operation.SourceLocalItem).First();
                    if (project != null)
                    {
                        found = true;
                        FileHelper.FileMove(operation.SourceLocalItem, operation.TargetLocalItem);
                        ProjectMoveFile(project, operation.SourceLocalItem, operation.TargetLocalItem);                  
                    }
                }
                else
                {
                    var project = FindProjectContainingFolder(operation.SourceLocalItem);
                    if (project != null)
                    {
                        found = true;
                        FileHelper.FolderMove(operation.SourceLocalItem, operation.TargetLocalItem);
                        ProjectMoveFolder(project, operation.SourceLocalItem, operation.TargetLocalItem);
                    }
                }
                if (!found)
                {
                    if (operation.ItemType == ItemType.File)
                    {
                        FileHelper.FileMove(operation.SourceLocalItem, operation.TargetLocalItem);
                    }
                    else if (operation.ItemType == ItemType.Folder)
                    {
                        FileHelper.FolderMove(operation.SourceLocalItem, operation.TargetLocalItem);
                    }
                }
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

        void ProcessGetOperations(List<GetOperation> getOperations, ProcessType processType, MessageDialogProgressMonitor monitor = null)
        {
            if (getOperations == null || getOperations.Count == 0)
                return;
         
            var downloadService = VersionControlService.Collection.GetService<VersionControlDownloadService>();
           
            try
            {
                UpdateLocalVersionQueue updates = new UpdateLocalVersionQueue(this);

                foreach (var operation in getOperations)
                {
                    try
                    {
                        monitor?.BeginTask(processType + " " + operation.TargetLocalItem, 1);
             
                        UpdateLocalVersion update = null;

                        switch (processType)
                        {
                            case ProcessType.Add:
                                update = ProcessAdd(operation, ProcessDirection.Normal);
                                break;
                            case ProcessType.Edit:
                                update = ProcessEdit(operation, downloadService, ProcessDirection.Normal);
                                break;
                            case ProcessType.Get:
                                monitor?.Log.WriteLine("Get: " + operation.VersionServer.ToString());
                                update = ProcessGet(operation, downloadService, ProcessDirection.Normal);
                                break;
                            case ProcessType.Rename:
                                update = ProcessRename(operation, ProcessDirection.Normal);
                                break;
                            case ProcessType.Delete:
                            case ProcessType.DeleteKeep:
                                update = ProcessDelete(operation, downloadService, ProcessDirection.Normal, processType);
                                break;
                            default:
                                update = ProcessGet(operation, downloadService, ProcessDirection.Normal);
                                break;
                        }

                        if (update != null)
                            updates.QueueUpdate(update);
                    }
                    finally
                    {
                        monitor?.EndTask();
                    }
                }

                updates.Flush();
                monitor?.EndTask();
            }
            finally
            {
                if (monitor != null)
                    monitor.Dispose();
            }
        }

        void UndoGetOperations(List<GetOperation> getOperations)
        {
            if (getOperations == null || getOperations.Count == 0)
                return;

            var downloadService = VersionControlService.Collection.GetService<VersionControlDownloadService>();


            UpdateLocalVersionQueue updates = new UpdateLocalVersionQueue(this);
            foreach (var operation in getOperations)
            {
                string stepName = operation.ChangeType == ChangeType.None ? "Undo " : operation.ChangeType.ToString();
                UpdateLocalVersion update = null;

                if (operation.IsAdd)
                {
                    update = ProcessAdd(operation, ProcessDirection.Undo);
                    if (update != null)
                        updates.QueueUpdate(update);
                    continue;
                }

                if (operation.IsDelete)
                {
                    update = ProcessDelete(operation, downloadService, ProcessDirection.Undo, ProcessType.Delete);
                    if (update != null)
                        updates.QueueUpdate(update);
                }

                if (operation.IsRename)
                {
                    update = ProcessRename(operation, ProcessDirection.Undo);

                    if (update != null)
                        updates.QueueUpdate(update);
                }
                if (operation.IsEdit || operation.IsEncoding)
                {
                    update = ProcessEdit(operation, downloadService, ProcessDirection.Undo);
                    if (update != null)
                        updates.QueueUpdate(update);
                }
            }

            updates.Flush();
        }

        #endregion

        public string GetItemContent(Item item)
        {
            if (item == null || item.ItemType == ItemType.Folder)
                return string.Empty;
            if (item.DeletionId > 0)
                return string.Empty;
            var dowloadService = this.ProjectCollection.GetService<VersionControlDownloadService>();
            var tempName = dowloadService.DownloadToTemp(item.ArtifactUri);
            var text = item.Encoding > 0 ? File.ReadAllText(tempName, Encoding.GetEncoding(item.Encoding)) :
                       File.ReadAllText(tempName);
            FileHelper.FileDelete(tempName);

            return text;
        }

        public string Comment { get; private set; }

        public string Computer { get; private set; }

        public List<WorkingFolder> Folders { get; private set; }

        public string Name { get; private set; }

        public DateTime LastAccessDate { get; private set; }

        public string OwnerName { get; private set; }

        public TeamFoundation.Client.ProjectCollection ProjectCollection { get; set; }

        public RepositoryService VersionControlService { get; set; }

        public override string ToString()
        {
            return "Owner: " + OwnerName + ", Name: " + Name;
        }
    }
}