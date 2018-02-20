using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Gui.Pads;

namespace MonoDevelop.VersionControl.TFS
{
    public class TeamFoundationServerVersionControl : VersionControlSystem
    {
        readonly Dictionary<FilePath, TeamFoundationServerRepository> _repositoriesCache = 
            new Dictionary<FilePath, TeamFoundationServerRepository>();

        public TeamFoundationServerVersionControl()
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                var pad = Ide.IdeApp.Workbench.GetPad<TeamExplorerPad>();
               
                if (pad != null)
                {
                    pad.Destroy();
                }
            }
        }

        #region implemented abstract members of VersionControlSystem

        protected override Repository OnCreateRepositoryInstance()
        {
            return new TeamFoundationServerRepository(null, null);
        }

        public override IRepositoryEditor CreateRepositoryEditor(Repository repo)
        {
            throw new NotImplementedException();
        }

        protected override FilePath OnGetRepositoryPath(FilePath path, string id)
        {
            return path;
        }

        public override string Name { get { return "TFS"; } }

        #endregion

        public override bool IsInstalled { get { return true; } }

        public override Repository GetRepositoryReference(FilePath path, string id)
        {
            if (path.IsNullOrEmpty)
                return null;
            
            foreach (var repo in _repositoriesCache)
            {
                if (repo.Key == path || path.IsChildPathOf(repo.Key))
                {
                    repo.Value.Refresh();
                    return repo.Value;
                }
                if (repo.Key.IsChildPathOf(path))
                {
                    _repositoriesCache.Remove(repo.Key);
                    var repo1 = GetRepository(path, id);
                    _repositoriesCache.Add(path, repo1);

                    return repo1;
                }
            }
            var repository = GetRepository(path, id);
            if (repository != null)
                _repositoriesCache.Add(path, repository);
            
            return repository;
        }

        TeamFoundationServerRepository GetRepository(FilePath path, string id)
        {
            var solutionPath = Path.ChangeExtension(Path.Combine(path, id), "sln");
            if (File.Exists(solutionPath)) 
            {
                var repo = FindBySolution(solutionPath);
                if (repo != null)
                    return repo;
                else
                    return FindByPath(path);
            }
            else
            {
                return FindByPath(path);
            }
        }

        TeamFoundationServerRepository FindBySolution(FilePath solutionPath)
        {
            var content = File.ReadAllLines(solutionPath);
            var line = content.FirstOrDefault(x => x.IndexOf("SccTeamFoundationServer", System.StringComparison.OrdinalIgnoreCase) > -1);
          
            if (line == null)
                return null;
            
            var parts = line.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
           
            if (parts.Length != 2)
                return null;
            
            var serverPath = new Uri(parts[1].Trim());
           
            foreach (var server in TeamFoundationServerClient.Settings.GetServers())
            {
                if (string.Equals(serverPath.Host, server.Uri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    var repo = GetRepoFromServer(server, solutionPath);
                    if (repo != null)
                        return repo;
                }
            }

            return null;
        }

        TeamFoundationServerRepository FindByPath(FilePath path)
        {
            foreach (var server in TeamFoundationServerClient.Settings.GetServers())
            {
                var repo = GetRepoFromServer(server, path);
              
                if (repo != null)
                    return repo;
            }

            return null;
        }

        TeamFoundationServerRepository GetRepoFromServer(BaseTeamFoundationServer server, FilePath path)
        {
            foreach (var collection in server.ProjectCollections)
            {
                var workspaces = TeamFoundationServerClient.Instance.GetWorkspaces(collection);
                var workspace = workspaces.SingleOrDefault(w => w.IsLocalPathMapped(path));
                if (workspace != null)
                {
                    var result = new TeamFoundationServerRepository(workspace.VersionControlService, path);
                    result.AttachWorkspace(workspace);

                    return result;
                }
            }

            return null;
        }

        internal void RefreshRepositories()
        {
            foreach (var repo in _repositoriesCache)
            {
                repo.Value.Refresh();
            }
        }
    }
}