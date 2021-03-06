// SoapInvoker.cs
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
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using Autofac;
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Services
{
	/// <summary>
    /// SOAP invoker.
    /// </summary>
    internal sealed class SoapInvoker : ISoapInvoker
    {
        readonly XNamespace xsiNs = XmlSchema.InstanceNamespace;
        readonly XNamespace xsdNs = XmlSchema.Namespace;
        readonly XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        
		readonly TeamFoundationServerService service;
        readonly ILoggingService _loggingService;
        readonly XNamespace messagegNs;
        readonly Uri url;
        readonly XDocument document;
        string methodName;

		public SoapInvoker(TeamFoundationServerService service, ILoggingService loggingService)
        {
            this.service = service;
            _loggingService = loggingService;
            url = service.Url;
            messagegNs = service.MessageNs;
            document = new XDocument(
                new XDeclaration("1.0", "utf-8", "no"),
                new XElement(soapNs + "Envelope", 
                    new XAttribute(XNamespace.Xmlns + "xsi", xsiNs),
                    new XAttribute(XNamespace.Xmlns + "xsd", xsdNs),
                    new XAttribute(XNamespace.Xmlns + "soap", soapNs)));
        }

        /// <summary>
        /// Create the envelope.
        /// </summary>
        /// <returns>The envelope.</returns>
        /// <param name="methodName">Method name.</param>
        public XElement CreateEnvelope(string methodName)
        {
            this.methodName = methodName;
            var innerMessage = new XElement(messagegNs + methodName);
            document.Root.Add(new XElement(soapNs + "Body", innerMessage));
         
            return innerMessage;
        }

        /// <summary>
        /// Create the envelope.
        /// </summary>
        /// <returns>The envelope.</returns>
        /// <param name="methodName">Method name.</param>
        /// <param name="headerName">Header name.</param>
        public SoapEnvelope CreateEnvelope(string methodName, string headerName)
        {
            this.methodName = methodName;
            var headerMessage = new XElement(messagegNs + headerName);
            var bodyMessage = new XElement(messagegNs + methodName);
            document.Root.Add(new XElement(soapNs + "Header", headerMessage));
            document.Root.Add(new XElement(soapNs + "Body", bodyMessage));
          
            return new SoapEnvelope { Header = headerMessage, Body = bodyMessage };
        }

        public XElement MethodResultExtractor(XElement responseElement)
        {
            return responseElement.Element(messagegNs + (methodName + "Result"));
        }

        /// <summary>
        /// Invokes the result.
        /// </summary>
        /// <returns>The result.</returns>
        public XElement InvokeResult()
		{
			// If OAuth Token from cache is near to expire, renew it.
			DependencyContainer.Container.Resolve<TeamFoundationServerVersionControlService>().RefreshAccessToken();

            var responseElement = InvokeResponse();
            return MethodResultExtractor(responseElement);
        }

        /// <summary>
        /// Invokes the response.
        /// </summary>
        /// <returns>The response.</returns>
        public XElement InvokeResponse()
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("Date: " + DateTime.Now.ToString("s"));
            logBuilder.AppendLine("Request:");
            logBuilder.AppendLine(this.ToString());

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                service.Server.Authorization.Authorize(request);
                request.AllowWriteStreamBuffering = true;
                request.Method = "POST";
                request.ContentType = "text/xml; charset=utf-8";
                request.Headers["SOAPAction"] = messagegNs.NamespaceName.TrimEnd('/') + '/' + methodName;

                document.Save(request.GetRequestStream());

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            var responseTxt = sr.ReadToEnd();
                            logBuilder.AppendLine("Response:");

                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                logBuilder.AppendLine(responseTxt);
                                throw new Exception("Error!!!\n" + responseTxt);
                            }
                            else
                            {
                                var resultDocument = XDocument.Parse(responseTxt);
                                logBuilder.AppendLine(resultDocument.ToString());
                                return resultDocument.Root.Element(soapNs + "Body").Element(messagegNs + (methodName + "Response"));
                            }
                        }
                    }
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null && wex.Response.ContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    XDocument doc = XDocument.Load(wex.Response.GetResponseStream());
                    logBuilder.AppendLine(doc.ToString());
                    throw new Exception(doc.ToString()); 
                }
                else
                {
                    logBuilder.AppendLine(wex.Message);
                    logBuilder.AppendLine(wex.StackTrace);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine(ex.Message);
                logBuilder.AppendLine(ex.StackTrace);
                throw;
            }
            finally
            {
                _loggingService.LogToDebug(logBuilder.ToString());
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(document.ToString());

            return builder.ToString();
        }
    }
}