﻿// TFSService.cs
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
using System.Xml;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Helpers;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
    /// Team foundation server service.
    /// </summary>
	public abstract class TeamFoundationServerService
    {
		protected TeamFoundationServerService(Uri baseUri, string servicePath)
        {
            BaseUri = baseUri;
            ServicePath = servicePath;
            Url = BaseUri.AddPath(ServicePath);
        }

        public Uri BaseUri { get; private set; }

        public string ServicePath { get; private set; }

        public Uri Url { get; private set; }

        public TeamFoundationServer Server { get; set; }

        public abstract XNamespace MessageNs { get; }

        public IXmlNamespaceResolver NsResolver
        {
            get
            {
                XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());
                manager.AddNamespace("msg", MessageNs.ToString());

                return manager;
            }
        }

        protected ISoapInvoker GetSoapInvoker()
        {
            return DependencyContainer.GetSoapInvoker(this);
        }
    }
}