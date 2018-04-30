// TeamFoundationServerVersionControlService.cs
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityService.Clients.ActiveDirectory;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS
{
	public sealed class TeamFoundationServerVersionControlService
    {
		/// <summary>
        /// The OAuth expiration margin in minutes.
        /// </summary>
        const int ExpirationMarginInMinutes = 5;

        readonly IConfigurationService _configurationService;
        readonly Configuration _configuration;

        public TeamFoundationServerVersionControlService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _configuration = _configurationService.Load();
        }

        public void AddServer(TeamFoundationServer server)
        {
            if (HasServer(server.Uri))
                RemoveServer(server.Uri);
            
            _configuration.Servers.Add(server);
            ServersChange();
        }

        public void RemoveServer(Uri url)
        {
            if (!HasServer(url))
                return;
            
            _configuration.Servers.RemoveAll(s => s.Uri == url);
            ServersChange();
        }

        public TeamFoundationServer GetServer(Uri url)
        {
            return _configuration.Servers.SingleOrDefault(s => s.Uri == url);
        }

        public bool HasServer(Uri url)
        {
            return _configuration.Servers.Any(s => s.Uri == url);
        }

        public IReadOnlyCollection<TeamFoundationServer> Servers { get { return _configuration.Servers; } }

        public event System.Action OnServersChange;

        public void ServersChange()
        {
            Save();
            OnServersChange?.Invoke();
        }

        public void SetActiveWorkspace(ProjectCollection collection, string workspaceName)
        {
            collection.ActiveWorkspaceName = workspaceName;
            Save();
        }       

        public LockLevel CheckOutLockLevel
        {
            get { return _configuration.DefaultLockLevel; }
            set
            {
                _configuration.DefaultLockLevel = value;
                Save();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this
        /// <see cref="T:MonoDevelop.VersionControl.TFS.TeamFoundationServerVersionControlService"/> debug mode.
		/// DebugMode write logs to local file.
        /// </summary>
        /// <value><c>true</c> if debug mode; otherwise, <c>false</c>.</value>
        public bool DebugMode
        {
            get
            {
                return _configuration.DebugMode;
            }
            set
            {
                _configuration.DebugMode = value;
                Save();
            }
        }

        /// <summary>
        /// Refreshs the working repositories.
        /// </summary>
        public void RefreshWorkingRepositories()
        {
            foreach(var system in VersionControlService.GetVersionControlSystems())
            {
                if (system is TeamFoundationServerVersionControl tfsSystem)
                {
                    tfsSystem.RefreshRepositories();
                }
            }
        }
       
        /// <summary>
        /// Refreshs the OAuth access token.
        /// </summary>
        /// <returns>True if renewed access token.</returns>
		public void RefreshAccessToken()
        {         
			var oAuthServers = _configuration.Servers.Where(s => s.Authorization is OAuthAuthorization).ToList();

			foreach (var server in oAuthServers)
			{
				var auth = (OAuthAuthorization)server.Authorization;

				bool tokenNearExpiry = (auth.ExpiresOn <=
										DateTime.UtcNow + TimeSpan.FromMinutes(ExpirationMarginInMinutes));

				if (tokenNearExpiry)
				{
					var tokenCache = AdalCacheHelper.GetAdalFileCacheInstance(server.Uri.Host);

					var items = tokenCache.ReadItems();

					if (items.Any())
					{
						var context = new AuthenticationContext(OAuthConstants.Authority, true, tokenCache);
                        
						// Acquires security token without asking for user credential.
						var task = Task.Run(async () => await context.AcquireTokenSilentAsync(OAuthConstants.Resource, OAuthConstants.ClientId));
						task.Wait();

						auth.OauthToken = task.Result.AccessToken;
						auth.ExpiresOn = task.Result.ExpiresOn;

						server.Authorization = auth;

                        // Update cache
						AddServer(server);
						AdalCacheHelper.PersistTokenCache(server.Uri.Host, tokenCache);
					}
				}
			}         
		}

        /// <summary>
        /// Save configuration.
        /// </summary>
        void Save()
        {
            _configurationService.Save(_configuration);
        }
    }
}