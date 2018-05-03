// TeamFoundationServerVersionControl.cs
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
using Autofac;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.TFS.Gui.Pads;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS
{   
	/// <summary>
    /// Team foundation server version control.
    /// </summary>
    public class TeamFoundationServerVersionControl : VersionControlSystem
    {
        readonly Dictionary<FilePath, TeamFoundationServerRepository> _repositoriesCache = new Dictionary<FilePath, TeamFoundationServerRepository>();
        readonly TeamFoundationServerVersionControlService _versionControlService;

        public TeamFoundationServerVersionControl()
        {
            if (VersionControlService.IsGloballyDisabled)
            {
                var pad = IdeApp.Workbench.GetPad<TeamExplorerPad>();

                if (pad != null)
                {
                    pad.Destroy();
                }
            }
            else
            {
                DependencyContainer.Register(new ServiceBuilder());
                _versionControlService = DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>();
            }
        }

        #region Implemented abstract members of VersionControlSystem

        /// <summary>
        /// Create a repository instance.
        /// </summary>
        /// <returns>The create repository instance.</returns>
        protected override Repository OnCreateRepositoryInstance()
        {
			return DependencyContainer.GetTeamFoundationServerRepository(null, null, null);
        }

        public override IRepositoryEditor CreateRepositoryEditor(Repository repo)
        {
            return null;
        }

        /// <summary>
        /// Gets the repository name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name { get { return "TFS"; } }

        #endregion

        public override bool IsInstalled { get { return true; } }

        /// <summary>
        /// Gets the repository reference.
        /// </summary>
        /// <returns>The repository reference.</returns>
        /// <param name="path">Path.</param>
        /// <param name="id">Identifier.</param>
        public override Repository GetRepositoryReference(FilePath path, string id)
        {
			if (path.IsNullOrEmpty)
			{
				return null;
			}

			foreach (var repo in _repositoriesCache)
            {
				if (repo.Value != null)
				{
					if (repo.Key == path || path.IsChildPathOf(repo.Key))
					{                  
						return repo.Value;
					}

					if (repo.Key.IsChildPathOf(path))
					{
						_repositoriesCache.Remove(repo.Key);
						var repoClone = GetRepository(path, id);
						_repositoriesCache.Add(path, repoClone);

						return repoClone;
					}
				}
            }

            var repository = GetRepository(path, id);

			if (repository != null)
			{
				_repositoriesCache.Add(path, repository);
			}

            return repository;
        }

        /// <summary>
        /// Get repository path.
        /// </summary>
        /// <returns>The get repository path.</returns>
        /// <param name="path">Path.</param>
        /// <param name="id">Identifier.</param>
		protected override FilePath OnGetRepositoryPath(FilePath path, string id)
        {
			if (path.IsEmpty || path.ParentDirectory.IsEmpty || path.IsNull || path.ParentDirectory.IsNull)
			{
				return string.Empty;
			}

            if (Directory.Exists(path))
			{
				return path;
			}

            return OnGetRepositoryPath(path.ParentDirectory, id);
        }
        
        /// <summary>
        /// Gets the repository by file path.
        /// </summary>
        /// <returns>The repository.</returns>
        /// <param name="path">Path.</param>
        /// <param name="id">Identifier.</param>
        TeamFoundationServerRepository GetRepository(FilePath path, string id)
        {
            var solutionPath = Path.ChangeExtension(Path.Combine(path, id), "sln");

			if (File.Exists(solutionPath)) //Read Solution
			{
				var repo = FindBySolution(solutionPath);
				return repo ?? FindByPath(path);
			}

			return FindByPath(path);
		}

        /// <summary>
        /// Finds the repository by solution path.
        /// </summary>
        /// <returns>The by solution.</returns>
        /// <param name="solutionPath">Solution path.</param>
        TeamFoundationServerRepository FindBySolution(FilePath solutionPath)
        {
            var content = File.ReadAllLines(solutionPath);
            var line = content.FirstOrDefault(x => x.IndexOf("SccTeamFoundationServer", StringComparison.OrdinalIgnoreCase) > -1);

            if (line == null)
                return null;
            
            var parts = line.Split(new [] { "=" }, StringSplitOptions.RemoveEmptyEntries);
         
            if (parts.Length != 2)
                return null;
            
            var serverPath = new Uri(parts[1].Trim());
          
            foreach (var server in _versionControlService.Servers)
            {
                if (string.Equals(serverPath.Host, server.Uri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    var repo = GetRepoFromServer(server, solutionPath);

					if (repo != null)
					{
						return repo;
					}
                }
            }

            return null;
        }

        /// <summary>
        /// Finds repository by path.
        /// </summary>
        /// <returns>The by path.</returns>
        /// <param name="path">Path.</param>
        TeamFoundationServerRepository FindByPath(FilePath path)
        {
            foreach (var server in _versionControlService.Servers)
            {
                var repo = GetRepoFromServer(server, path);

				if (repo != null)
				{
					System.Diagnostics.Debug.WriteLine(repo.GetHashCode());

					return repo;
				}
            }

            return null;
        }

        /// <summary>
        /// Get the repository.
        /// </summary>
        /// <returns>The repo from server.</returns>
        /// <param name="server">Server.</param>
        /// <param name="path">Path.</param>
        TeamFoundationServerRepository GetRepoFromServer(TeamFoundationServer server, FilePath path)
        {
            foreach (var projectCollection in server.ProjectCollections)
            {
                var workspaceDatas = projectCollection.GetLocalWorkspaces();
                var workspaceData = workspaceDatas.SingleOrDefault(w => w.IsLocalPathMapped(new LocalPath(path)));
              
                if (workspaceData != null)
                {
					return DependencyContainer.GetTeamFoundationServerRepository(path, workspaceData, projectCollection);
				}
            }           

            return null;
        }

        /// <summary>
        /// Refreshs the repositories.
        /// </summary>
        internal void RefreshRepositories()
        {
			foreach (var repo in _repositoriesCache)
            {
                repo.Value.Refresh();
            }
        }       
    }
}