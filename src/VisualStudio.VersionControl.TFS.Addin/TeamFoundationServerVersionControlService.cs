// TFSVersionControlService.cs
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
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.Services;
using MonoDevelop.VersionControl.TFS.VersionControl.Enums;

namespace MonoDevelop.VersionControl.TFS
{
    public sealed class TeamFoundationServerVersionControlService
    {
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

        public MergeToolInfo MergeToolInfo
        {
            get { return _configuration.MergeToolInfo; }
            set
            {
                _configuration.MergeToolInfo = value;
                Save();
            }
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

        public bool IsDebugMode
        { 
            get
            {
                return _configuration.IsDebugMode;
            }
            set
            {
                _configuration.IsDebugMode = value;
                Save();
            }
        }

        public void RefreshWorkingRepositories()
        {
            foreach(var system in VersionControlService.GetVersionControlSystems())
            {
                var tfsSystem = system as TeamFoundationServerVersionControl;

                if (tfsSystem != null)
                {
                    tfsSystem.RefreshRepositories();
                }
            }
        }

        void Save()
        {
            _configurationService.Save(_configuration);
        }
    }
}