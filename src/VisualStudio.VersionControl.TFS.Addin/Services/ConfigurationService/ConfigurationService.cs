﻿// ConfigurationService.cs
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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
	/// Configuration service.
	/// </summary>
	public sealed class ConfigurationService : IConfigurationService
    {
        const string ConfigName = "MonoDevelop.VersionControl.TFS.config";
        LocalPath _configurationPath;

        public void Init(string configurationPath)
        {
            _configurationPath = Path.Combine(configurationPath, ConfigName);
        }

        public void Save(Configuration configuration)
        {
            XDocument doc = new XDocument();
            doc.Add(new XElement("TFSConfiguration"));
            doc.Root.Add(new XElement("Servers", configuration.Servers.Select(x => x.ToConfigXml())));
            doc.Root.Add(new XElement("DefaultLockLevel", (int)configuration.DefaultLockLevel));
            doc.Root.Add(new XElement("DebugMode", configuration.DebugMode));

            doc.Save(_configurationPath);
        }

        public Configuration Load()
        {
            var configuration = Configuration.Default();
            if (!_configurationPath.Exists)
                return configuration;
            
            var document = XDocument.Load(_configurationPath);
            if (document.Root == null)
                return configuration;

            configuration.Servers.AddRange(document.XPathSelectElements("//Servers/Server").Select(TeamFoundationServer.FromConfigXml));            

            var lockLevelElement = document.Root.Element("DefaultLockLevel");
           
            if (lockLevelElement != null)
                configuration.DefaultLockLevel = (LockLevel) Convert.ToInt32(lockLevelElement.Value);

            var isDebugElement = document.Root.Element("DebugMode");

            if (isDebugElement != null)
                configuration.DebugMode = Convert.ToBoolean(isDebugElement.Value);
            
            return configuration;
        }
    }
}