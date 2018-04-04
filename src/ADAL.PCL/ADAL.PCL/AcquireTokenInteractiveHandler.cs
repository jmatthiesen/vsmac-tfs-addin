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
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.IdentityService.Clients.ActiveDirectory
{
    internal class AcquireTokenInteractiveHandler : AcquireTokenHandlerBase
    {
        internal AuthorizationResult authorizationResult;

        readonly Uri _redirectUri;
        readonly string _redirectUriRequestParameter;
        readonly IPlatformParameters _authorizationParameters;
        readonly string _extraQueryParameters;
        readonly IWebUI _webUi;
        readonly UserIdentifier _userId;

        public AcquireTokenInteractiveHandler(Authenticator authenticator, TokenCache tokenCache, string resource, string clientId, Uri redirectUri, IPlatformParameters parameters, UserIdentifier userId, string extraQueryParameters, IWebUI webUI)
            : base(authenticator, tokenCache, resource, new ClientKey(clientId), TokenSubjectType.User)
        {
            _redirectUri = PlatformPlugin.PlatformInformation.ValidateRedirectUri(redirectUri, CallState);

            if (!string.IsNullOrWhiteSpace(redirectUri.Fragment))
            {
                throw new ArgumentException(AdalErrorMessage.RedirectUriContainsFragment, "redirectUri");
            }

            _authorizationParameters = parameters;

            _redirectUriRequestParameter = PlatformPlugin.PlatformInformation.GetRedirectUriAsString(redirectUri, CallState);
            _userId = userId ?? throw new ArgumentNullException("userId", AdalErrorMessage.SpecifyAnyUser);

            if (!string.IsNullOrEmpty(extraQueryParameters) && extraQueryParameters[0] == '&')
            {
                extraQueryParameters = extraQueryParameters.Substring(1);
            }

            _extraQueryParameters = extraQueryParameters;
            _webUi = webUI;
            UniqueId = _userId.UniqueId;
            DisplayableId = _userId.DisplayableId;
            UserIdentifierType = _userId.Type;
            LoadFromCache = (tokenCache != null && parameters != null && PlatformPlugin.PlatformInformation.GetCacheLoadPolicy(parameters));
            SupportADFS = true;
            CacheQueryData.DisplayableId = DisplayableId;
            CacheQueryData.UniqueId = UniqueId;

            brokerParameters["force"] = "NO";
            if (_userId != UserIdentifier.AnyUser)
            {
                brokerParameters["username"] = userId.Id;
            }
            else
            {
                brokerParameters["username"] = string.Empty;
            }
            brokerParameters["username_type"] = userId.Type.ToString();

            brokerParameters["redirect_uri"] = redirectUri.AbsoluteUri;
            brokerParameters["extra_qp"] = extraQueryParameters;
            PlatformPlugin.BrokerHelper.PlatformParameters = _authorizationParameters;
        }

        protected override async Task PreTokenRequest()
        {
            await base.PreTokenRequest();

            // We do not have async interactive API in .NET, so we call this synchronous method instead.
            await AcquireAuthorizationAsync();
            VerifyAuthorizationResult();
        }

        internal async Task AcquireAuthorizationAsync()
        {
            Uri authorizationUri = CreateAuthorizationUri();
            authorizationResult = await _webUi.AcquireAuthorizationAsync(authorizationUri, _redirectUri, CallState);
        }

        internal async Task<Uri> CreateAuthorizationUriAsync(Guid correlationId)
        {
            CallState.CorrelationId = correlationId;
            await Authenticator.UpdateFromTemplateAsync(CallState);
            return CreateAuthorizationUri();
        }
        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.AuthorizationCode;
            requestParameters[OAuthParameter.Code] = authorizationResult.Code;
            requestParameters[OAuthParameter.RedirectUri] = _redirectUriRequestParameter;
        }

        protected override void PostTokenRequest(AuthenticationResultEx resultEx)
        {
            base.PostTokenRequest(resultEx);
        
            if ((DisplayableId == null && UniqueId == null) || UserIdentifierType == UserIdentifierType.OptionalDisplayableId)
            {
                return;
            }

            string uniqueId = (resultEx.Result.UserInfo != null && resultEx.Result.UserInfo.UniqueId != null) ? resultEx.Result.UserInfo.UniqueId : "NULL";
            string displayableId = (resultEx.Result.UserInfo != null) ? resultEx.Result.UserInfo.DisplayableId : "NULL";

            if (UserIdentifierType == UserIdentifierType.UniqueId && string.Compare(uniqueId, UniqueId, StringComparison.Ordinal) != 0)
            {
                throw new AdalUserMismatchException(UniqueId, uniqueId);
            }

            if (UserIdentifierType == UserIdentifierType.RequiredDisplayableId && string.Compare(displayableId, DisplayableId, StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new AdalUserMismatchException(DisplayableId, displayableId);
            }
        }

        Uri CreateAuthorizationUri()
        {
            string loginHint = null;

            if (!_userId.IsAnyUser
                && (_userId.Type == UserIdentifierType.OptionalDisplayableId
                    || _userId.Type == UserIdentifierType.RequiredDisplayableId))
            {
                loginHint = _userId.Id;
            }

            IRequestParameters requestParameters = CreateAuthorizationRequest(loginHint);

            return  new Uri(new Uri(Authenticator.AuthorizationUri), "?" + requestParameters);
        }

        DictionaryRequestParameters CreateAuthorizationRequest(string loginHint)
        {
            var authorizationRequestParameters = new DictionaryRequestParameters(Resource, ClientKey);
            authorizationRequestParameters[OAuthParameter.ResponseType] = OAuthResponseType.Code;
            authorizationRequestParameters[OAuthParameter.HasChrome] = "1";
            authorizationRequestParameters[OAuthParameter.RedirectUri] = _redirectUriRequestParameter;

            if (!string.IsNullOrWhiteSpace(loginHint))
            {
                authorizationRequestParameters[OAuthParameter.LoginHint] = loginHint;
            }

            if (CallState != null && CallState.CorrelationId != Guid.Empty)
            {
                authorizationRequestParameters[OAuthParameter.CorrelationId] = CallState.CorrelationId.ToString();
            }

            if (_authorizationParameters != null)
            {
                PlatformPlugin.PlatformInformation.AddPromptBehaviorQueryParameter(_authorizationParameters, authorizationRequestParameters);
            }

            if (PlatformPlugin.HttpClientFactory.AddAdditionalHeaders)
            {
                IDictionary<string, string> adalIdParameters = AdalIdHelper.GetAdalIdParameters();
                foreach (KeyValuePair<string, string> kvp in adalIdParameters)
                {
                    authorizationRequestParameters[kvp.Key] = kvp.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(_extraQueryParameters))
            {
                // Checks for extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(_extraQueryParameters, '&', false, CallState);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (authorizationRequestParameters.ContainsKey(kvp.Key))
                    {
                        throw new AdalException(AdalError.DuplicateQueryParameter, string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DuplicateQueryParameterTemplate, kvp.Key));
                    }
                }

                authorizationRequestParameters.ExtraQueryParameter = _extraQueryParameters;
            }

            return authorizationRequestParameters;
        }

        void VerifyAuthorizationResult()
        {
            if (authorizationResult.Error == OAuthError.LoginRequired)
            {
                throw new AdalException(AdalError.UserInteractionRequired);
            }

            if (authorizationResult.Status != AuthorizationStatus.Success)
            {
                throw new AdalServiceException(authorizationResult.Error, authorizationResult.ErrorDescription);
            }
        }

        protected override void UpdateBrokerParameters(IDictionary<string, string> parameters)
        {
            Uri uri = new Uri(authorizationResult.Code);
            string query = EncodingHelper.UrlDecode(uri.Query);
            Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(query, '&', false, CallState);
            parameters["username"] = kvps["username"];
        }

        protected override bool BrokerInvocationRequired()
        {
            if (authorizationResult != null
                && !string.IsNullOrEmpty(authorizationResult.Code)
                && authorizationResult.Code.StartsWith("msauth://", StringComparison.CurrentCultureIgnoreCase))
            {
                brokerParameters["broker_install_url"] = authorizationResult.Code;
                return true;
            }

            return false;
        }
    }
}