// BasicAuthorization.cs
// 
// Author:
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2018 Javier Suárez Ruiz
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
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Helpers;

namespace MonoDevelop.VersionControl.TFS.Models
{
    sealed class OAuthAuthorization : UserPasswordAuthorization, IServerAuthorization
    {
        OAuthAuthorization()
        {

        }

        public string OauthToken { get; set; }
        public DateTimeOffset ExpiresOn { get; set; }

        public void Authorize(HttpWebRequest request)
        {
            request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + OauthToken);
        }

        public void Authorize(WebClient client)
        {
            client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + OauthToken);
        }

        public void Authorize(HttpClientHandler clientHandler, HttpRequestMessage message)
        {
            // TODO:
        }

        public static OAuthAuthorization FromConfigXml(XElement element, Uri serverUri)
        {
            var auth = new OAuthAuthorization();

            auth.ReadConfig(element, serverUri);
            auth.OauthToken = element.GetAttributeValue("OauthToken");
            auth.ExpiresOn = DateTimeOffsetHelper.FromString(element.GetAttributeValue("ExpiresOn"));                  

            return auth;
        }

        public XElement ToConfigXml()
        {
            var element = new XElement("Oauth", 
                                       new XAttribute("OauthToken", OauthToken));
            element.Add(new XAttribute("ExpiresOn", ExpiresOn));

            return element;
        }

        public static OAuthAuthorization FromConfigWidget(IOAuthAuthorizationConfig config)
        {
            var authorization = new OAuthAuthorization();
            authorization.ReadConfig(config);

            authorization.OauthToken = config.OauthToken;
            authorization.ExpiresOn = config.ExpiresOn;

            return authorization;
        }
    }
}