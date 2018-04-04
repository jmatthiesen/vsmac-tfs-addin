//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.IdentityService.Clients.ActiveDirectory
{
    internal class ClientKey
    {
        public ClientKey(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException("clientId");
            }

            ClientId = clientId;
            HasCredential = false;
        }

        public ClientKey(ClientCredential clientCredential)
        {
            if (clientCredential == null)
            {
                throw new ArgumentNullException("clientCredential");
            }

            Credential = clientCredential;
            ClientId = clientCredential.ClientId;
            HasCredential = true;
        }

        public ClientKey(IClientAssertionCertificate clientCertificate, Authenticator authenticator)
        {
            Authenticator = authenticator;

            if (clientCertificate == null)
            {
                throw new ArgumentNullException("clientCertificate");
            }

            Certificate = clientCertificate;
            ClientId = clientCertificate.ClientId;
            HasCredential = true;
        }

        public ClientKey(ClientAssertion clientAssertion)
        {
            if (clientAssertion == null)
            {
                throw new ArgumentNullException("clientAssertion");
            }

            Assertion = clientAssertion;
            ClientId = clientAssertion.ClientId;
            HasCredential = true;
        }

        public ClientCredential Credential { get; set; }

        public IClientAssertionCertificate Certificate { get; set; }

        public ClientAssertion Assertion { get; set; }

        public Authenticator Authenticator { get; set; }

        public string ClientId { get; set; }

        public bool HasCredential { get; set; }


        public void AddToParameters(IDictionary<string, string> parameters)
        {
            if (ClientId != null)
            {
                parameters[OAuthParameter.ClientId] = ClientId;
            }

            if (Credential != null)
            {
                parameters[OAuthParameter.ClientSecret] = Credential.ClientSecret;
            }
            else if (Assertion != null)
            {
                parameters[OAuthParameter.ClientAssertionType] = Assertion.AssertionType;
                parameters[OAuthParameter.ClientAssertion] = Assertion.Assertion;
            }
            else if (Certificate != null)
            {
                JsonWebToken jwtToken = new JsonWebToken(Certificate, Authenticator.SelfSignedJwtAudience);
                ClientAssertion clientAssertion = jwtToken.Sign(Certificate);
                parameters[OAuthParameter.ClientAssertionType] = clientAssertion.AssertionType;
                parameters[OAuthParameter.ClientAssertion] = clientAssertion.Assertion;
            }
        }
    }
}
