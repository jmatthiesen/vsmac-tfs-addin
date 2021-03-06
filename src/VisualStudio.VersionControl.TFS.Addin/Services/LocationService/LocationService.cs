// LocationService.cs
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
using System.Linq;
using System.Reflection;
using System.Xml.XPath;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
    /// Location service.
    /// </summary>
	internal sealed class LocationService : TeamFoundationServerService
    {
        #region Implemented abstract members of TFSService

        public override System.Xml.Linq.XNamespace MessageNs
        {
            get
            {
                return TeamFoundationServerServiceMessage.ServiceNs;
            }
        }

        #endregion

        internal LocationService(Uri serverUri, string servicePath)
            : base(serverUri, servicePath)
        {
        }

		public TeamFoundationServerService LoadService(Type serviceType)
        {
            var invoker = GetSoapInvoker();
            invoker.CreateEnvelope("QueryServices");
            var resultEl = invoker.InvokeResult();

            var resolverAttribute = serviceType.GetCustomAttributes(typeof(ServiceResolverAttribute), false)
                                               .Cast<ServiceResolverAttribute>().SingleOrDefault();
          
            if (resolverAttribute == null)
                return null;
            
            var resolver = (IServiceResolver)Activator.CreateInstance(resolverAttribute.ResolverType);
                     
            var serviceElement = resultEl.XPathSelectElement(string.Format("./msg:ServiceDefinitions/msg:ServiceDefinition[@identifier='{0}']", resolver.Id), 
                this.NsResolver);
            
            if (serviceElement == null)
                throw new Exception("Service not found");

            var moniker = resultEl.Attribute("DefaultAccessMappingMoniker") != null ? 
                          resultEl.Attribute("DefaultAccessMappingMoniker").Value : "PublicAccessMapping";

            var accessElement = resultEl.XPathSelectElement(
                string.Format("./msg:AccessMappings/msg:AccessMapping[@Moniker='{0}']", moniker), 
                this.NsResolver
            );

            var basePath = new Uri(accessElement.Attribute("AccessPoint").Value);
            var servicePath = serviceElement.Attribute("relativePath").Value;

            var serviceConstructor = serviceType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(System.Uri), typeof(System.String) }, null);
			var service = (TeamFoundationServerService)serviceConstructor.Invoke(new object[] { basePath, servicePath } );

            service.Server = Server;
            var properties = serviceType.GetProperties();
            foreach (var property in properties)
            {
                var requiredAttribute = property.GetCustomAttributes(typeof(RequireServiceAttribute), false)
                                                .Cast<RequireServiceAttribute>().SingleOrDefault();
                if (requiredAttribute != null)
                {
                    var subService = LoadService(property.PropertyType);
                    property.SetValue(service, subService, new object[0]);
                }
            }

            return service;
        }


        public TService LoadService<TService>()
			where TService: TeamFoundationServerService
        {
            return (TService)LoadService(typeof(TService));
        }
    }
}