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

namespace Microsoft.IdentityService.Clients.ActiveDirectory
{
    class AcquireTokenByAuthorizationCodeHandler : AcquireTokenHandlerBase
    {
        readonly string _authorizationCode;
        readonly Uri _redirectUri;

        public AcquireTokenByAuthorizationCodeHandler(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, string authorizationCode, Uri redirectUri)
            : base(authenticator, tokenCache, resource ?? NullResource, clientKey, TokenSubjectType.UserPlusClient)
        {
            if (string.IsNullOrWhiteSpace(authorizationCode))
            {
                throw new ArgumentNullException("authorizationCode");
            }

            _authorizationCode = authorizationCode;
            _redirectUri = redirectUri ?? throw new ArgumentNullException("redirectUri");

            LoadFromCache = false;

            SupportADFS = true;
        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.AuthorizationCode;
            requestParameters[OAuthParameter.Code] = _authorizationCode;
            requestParameters[OAuthParameter.RedirectUri] = _redirectUri.OriginalString;
        }

        protected override void PostTokenRequest(AuthenticationResultEx resultEx)
        {
            base.PostTokenRequest(resultEx);
            UserInfo userInfo = resultEx.Result.UserInfo;
            UniqueId = (userInfo == null) ? null : userInfo.UniqueId;
            DisplayableId = (userInfo == null) ? null : userInfo.DisplayableId;
            if (resultEx.ResourceInResponse != null)
            {
                Resource = resultEx.ResourceInResponse;
                PlatformPlugin.Logger.Verbose(CallState, "Resource value in the token response was used for storing tokens in the cache");
            }

            // If resource is not passed as an argument and is not returned by STS either, 
            // we cannot store the token in the cache with null resource.
            // TODO: Store refresh token though if STS supports MRRT.
            StoreToCache = StoreToCache && (Resource != null);
        }
    }
}
