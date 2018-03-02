﻿using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.TeamFoundation.Client
{
    public class VisualStudioOnlineTFS : BaseTeamFoundationServer, IAuthServer
    {

        public VisualStudioOnlineTFS(Uri url, string name, string userName, string authUserName, string password, bool isPasswordSavedInXml)
            : base(url, name, userName, password, isPasswordSavedInXml)
        {
            AuthUserName = authUserName;
        }

        public static VisualStudioOnlineTFS FromLocalXml(XElement element, string password, bool isPasswordSavedInXml)
        {
            try
            {
                var server = new VisualStudioOnlineTFS(new Uri(element.Attribute("Url").Value),
                                                       element.Attribute("Name").Value,
                                                       element.Attribute("UserName").Value,
                                                       element.Attribute("AuthUserName").Value,
                                                       password,
                                                       isPasswordSavedInXml);
                server.ProjectCollections = element.Elements("ProjectCollection").Select(x => ProjectCollection.FromLocalXml(server, x)).ToList();
               
                return server;
            }
            catch
            {
                return null;
            }
        }

        public override XElement ToLocalXml()
        {
            var serverElement = new XElement("Server",
                                        new XAttribute("Type", (int)ServerType.VisualStudio),
                                        new XAttribute("Name", Name),
                                        new XAttribute("Url", Uri),
                                        new XAttribute("UserName", UserName),
                                        new XAttribute("AuthUserName", AuthUserName),
                
            ProjectCollections.Select(p => p.ToLocalXml()));
            
            if (IsPasswordSavedInXml)
                serverElement.Add(new XAttribute("Password", Password));
         
            return serverElement;
        }

        string AuthUserName { get; set; }

        public string AuthString 
        {
            get
            {
                var credentialBuffer = Encoding.UTF8.GetBytes(AuthUserName + ":" + Password);
               
                return "Basic " + Convert.ToBase64String(credentialBuffer);
            }
        }
    }
}