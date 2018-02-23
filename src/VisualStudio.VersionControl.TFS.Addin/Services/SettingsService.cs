using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client.Enums;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
    public class SettingsService
    {
        string ConfigFile { get { return UserProfile.Current.ConfigDir.Combine("VisualStudio.TFS.config"); } }
     
        CheckOutLockLevel _checkOutLockLevel;
        List<BaseTeamFoundationServer> _registredServers;
        Dictionary<string, string> _activeWorkspaces;

        public SettingsService()
        {
            Init();
        }

        public CheckOutLockLevel CheckOutLockLevel
        {
            get { return _checkOutLockLevel; }
            set
            {
                _checkOutLockLevel = value;
                SaveSettings();
            }
        }

        public MergeToolInfo MergeTool { get; set; }

        public List<BaseTeamFoundationServer> RegistredServers
        {
            get { return _registredServers; }
            set { _registredServers = value; }
        }

        public Dictionary<string, string> ActiveWorkspaces
        {
            get { return _activeWorkspaces; }
            set { _activeWorkspaces = value; }
        }

        void Init()
        {
            _registredServers = new List<BaseTeamFoundationServer>();
            _activeWorkspaces = new Dictionary<string, string>();
        }

        public void LoadSettings()
        {
            if (!File.Exists(ConfigFile))
                return;

            try
            {
                using (var file = File.OpenRead(ConfigFile))
                {
                    XDocument doc = XDocument.Load(file);
                    foreach (var serverElement in doc.Root.Element("Servers").Elements("Server"))
                    {
                        var isPasswordSavedInXml = serverElement.Attribute("Password") != null;
                        var password = isPasswordSavedInXml ? serverElement.Attribute("Password").Value : CredentialsHelper.GetPassword(new Uri(serverElement.Attribute("Url").Value));

                        if (password == null)
                            throw new Exception("TFS Addin: No Password found for TFS server: " + serverElement.Attribute("Name").Value);

                        var server = TeamFoundationServerFactory.Create(serverElement, password, isPasswordSavedInXml);
                       
                        if (server != null)
                            _registredServers.Add(server);
                    }
                    foreach (var workspace in doc.Root.Element("Workspaces").Elements("Workspace"))
                    {
                        _activeWorkspaces.Add(workspace.Attribute("Id").Value, workspace.Attribute("Name").Value);
                    }
                }
            }
            catch
            {
                return;
            }
        }

        public void AddServer(BaseTeamFoundationServer server)
        {
            if (HasServer(server.Name))
                RemoveServer(server.Name);
            
            _registredServers.Add(server);

            SaveSettings();
        }

        public void RemoveServer(string name)
        {
            if (!HasServer(name))
                return;
            
            _registredServers.RemoveAll(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
         
            SaveSettings();
        }

        public List<BaseTeamFoundationServer> GetServers()
        {
            return _registredServers;
        }

        public BaseTeamFoundationServer GetServer(string name)
        {
            return _registredServers
                .SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasServer(string name)
        {
            return _registredServers
                .Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public void SetActiveWorkspace(ProjectCollection collection, string workspaceName)
        {
            _activeWorkspaces[collection.Id] = workspaceName;
            SaveSettings();
        }

        public string GetActiveWorkspace(ProjectCollection collection)
        {
            if (!_activeWorkspaces.ContainsKey(collection.Id))
                return string.Empty;
            
            return _activeWorkspaces[collection.Id];
        }

        public void SaveSettings()
        {
            using (var file = File.Create(ConfigFile))
            {
                XDocument doc = new XDocument();
                doc.Add(new XElement("TFSRoot"));
                doc.Root.Add(new XElement("Servers", _registredServers.Select(x => x.ToLocalXml())));
                doc.Root.Add(new XElement("Workspaces", _activeWorkspaces.Select(a => new XElement("Workspace", new XAttribute("Id", a.Key), new XAttribute("Name", a.Value)))));
                doc.Root.Add(new XElement("CheckOutLockLevel", (int)CheckOutLockLevel));

                if (MergeTool != null)
                    doc.Root.Add(new XElement("MergeTool", new XAttribute("Command", MergeTool.CommandName), new XAttribute("Arguments", MergeTool.Arguments)));
                
                doc.Save(file);
                file.Close();
            }
        }
    }
}