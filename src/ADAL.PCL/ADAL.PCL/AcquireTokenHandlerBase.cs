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
    internal abstract class AcquireTokenHandlerBase
    {
        protected const string NullResource = "null_resource_as_optional";
        protected readonly static Task CompletedTask = Task.FromResult(false);
        readonly TokenCache _tokenCache;
        protected readonly IDictionary<string, string> brokerParameters;
        protected readonly CacheQueryData CacheQueryData = null;

        protected AcquireTokenHandlerBase(Authenticator authenticator, TokenCache tokenCache, string resource,
            ClientKey clientKey, TokenSubjectType subjectType)
        {
            Authenticator = authenticator;
            CallState = CreateCallState(Authenticator.CorrelationId);
            PlatformPlugin.Logger.Information(CallState,
                string.Format(CultureInfo.CurrentCulture, "=== Token Acquisition started:\n\tAuthority: {0}\n\tResource: {1}\n\tClientId: {2}\n\tCacheType: {3}\n\tAuthentication Target: {4}\n\t",
                authenticator.Authority, resource, clientKey.ClientId,
                (tokenCache != null) ? tokenCache.GetType().FullName + string.Format(CultureInfo.CurrentCulture, " ({0} items)", tokenCache.Count) : "null",
                subjectType));

            _tokenCache = tokenCache;

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentNullException("resource");
            }

            Resource = (resource != NullResource) ? resource : null;
            ClientKey = clientKey;
            TokenSubjectType = subjectType;

            LoadFromCache = (tokenCache != null);
            StoreToCache = (tokenCache != null);
            SupportADFS = false;

            brokerParameters = new Dictionary<string, string>();
            brokerParameters["authority"] = authenticator.Authority;
            brokerParameters["resource"] = resource;
            brokerParameters["client_id"] = clientKey.ClientId;
            brokerParameters["correlation_id"] = CallState.CorrelationId.ToString();
            brokerParameters["client_version"] = AdalIdHelper.GetAdalVersion();
            ResultEx = null;

            CacheQueryData = new CacheQueryData();
            CacheQueryData.Authority = Authenticator.Authority;
            CacheQueryData.Resource = Resource;
            CacheQueryData.ClientId = ClientKey.ClientId;
            CacheQueryData.SubjectType = TokenSubjectType;
            CacheQueryData.UniqueId = UniqueId;
            CacheQueryData.DisplayableId = DisplayableId;
        }

        internal CallState CallState { get; set; }

        protected bool SupportADFS { get; set; }

        protected Authenticator Authenticator { get; set; }

        protected string Resource { get; set; }

        protected ClientKey ClientKey { get; set; }

        protected AuthenticationResultEx ResultEx { get; set; }

        protected TokenSubjectType TokenSubjectType { get; set; }

        protected string UniqueId { get; set; }

        protected string DisplayableId { get; set; }

        protected UserIdentifierType UserIdentifierType { get; set; }

        protected bool LoadFromCache { get; set; }
        
        protected bool StoreToCache { get; set; }

        public async Task<AuthenticationResult> RunAsync()
        {
            bool notifiedBeforeAccessCache = false;

            try
            {
                await PreRunAsync();

                
                if (LoadFromCache)
                {
                    NotifyBeforeAccessCache();
                    notifiedBeforeAccessCache = true;

                    ResultEx = _tokenCache.LoadFromCache(CacheQueryData, CallState);
                    ValidateResult();

                    if (ResultEx != null && ResultEx.Result.AccessToken == null && ResultEx.RefreshToken != null)
                    {
                        ResultEx = await RefreshAccessTokenAsync(ResultEx);
                     
                        if (ResultEx != null && ResultEx.Exception == null)
                        {
                            _tokenCache.StoreToCache(ResultEx, Authenticator.Authority, Resource, ClientKey.ClientId, TokenSubjectType, CallState);
                        }
                    }
                }

                if (ResultEx == null || ResultEx.Exception != null)
                {
                    if (PlatformPlugin.BrokerHelper.CanInvokeBroker)
                    {
                        ResultEx = await PlatformPlugin.BrokerHelper.AcquireTokenUsingBroker(brokerParameters);
                    }
                    else
                    {
                        await PreTokenRequest();
                        
                        // Check if broker app installation is required for authentication.
                        if (BrokerInvocationRequired())
                        {
                            ResultEx = await PlatformPlugin.BrokerHelper.AcquireTokenUsingBroker(brokerParameters);
                        }
                        else
                        {
                            ResultEx = await SendTokenRequestAsync();
                        }
                    }

                    //broker token acquisition failed
                    if (ResultEx != null && ResultEx.Exception != null)
                    {
                        throw ResultEx.Exception;
                    }

                    PostTokenRequest(ResultEx);
                  
                    if (StoreToCache)
                    {
                        if (!notifiedBeforeAccessCache)
                        {
                            NotifyBeforeAccessCache();
                            notifiedBeforeAccessCache = true;
                        }

                        _tokenCache.StoreToCache(ResultEx, Authenticator.Authority, Resource, ClientKey.ClientId, TokenSubjectType, CallState);
                    }
                }

                await PostRunAsync(ResultEx.Result);
                return ResultEx.Result;
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Error(CallState, ex);
                throw;
            }
            finally
            {
                if (notifiedBeforeAccessCache)
                {
                    NotifyAfterAccessCache();
                }
            }
        }


        protected virtual void ValidateResult()
        {
           
        }

        protected virtual void UpdateBrokerParameters(IDictionary<string, string> parameters)
        {
            
        }

        protected virtual bool BrokerInvocationRequired()
        {
            return false;
        }

        public static CallState CreateCallState(Guid correlationId)
        {
            correlationId = (correlationId != Guid.Empty) ? correlationId : Guid.NewGuid();
            return new CallState(correlationId);
        }

        protected virtual Task PostRunAsync(AuthenticationResult result)
        {
            LogReturnedToken(result);

            return CompletedTask;
        }

        protected virtual async Task PreRunAsync()
        {
            await Authenticator.UpdateFromTemplateAsync(CallState);
            ValidateAuthorityType();
        }

        protected virtual Task PreTokenRequest()
        {
            return CompletedTask;
        }

        protected virtual void PostTokenRequest(AuthenticationResultEx result)
        {
            Authenticator.UpdateTenantId(result.Result.TenantId);
        }

        protected abstract void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters);

        protected virtual async Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            var requestParameters = new DictionaryRequestParameters(Resource, ClientKey);
            AddAditionalRequestParameters(requestParameters);
            return await SendHttpMessageAsync(requestParameters);
        }

        protected async Task<AuthenticationResultEx> SendTokenRequestByRefreshTokenAsync(string refreshToken)
        {
            var requestParameters = new DictionaryRequestParameters(Resource, ClientKey);
            requestParameters[OAuthParameter.GrantType] = OAuthGrantType.RefreshToken;
            requestParameters[OAuthParameter.RefreshToken] = refreshToken;
            requestParameters[OAuthParameter.Scope] = OAuthValue.ScopeOpenId;

            AuthenticationResultEx result = await SendHttpMessageAsync(requestParameters);

            if (result.RefreshToken == null)
            {
                result.RefreshToken = refreshToken;
                PlatformPlugin.Logger.Verbose(CallState, "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
            }

            return result;
        }

        async Task<AuthenticationResultEx> RefreshAccessTokenAsync(AuthenticationResultEx result)
        {
            AuthenticationResultEx newResultEx = null;

            if (Resource != null)
            {
                PlatformPlugin.Logger.Verbose(CallState, "Refreshing access token...");

                try
                {
                    newResultEx = await SendTokenRequestByRefreshTokenAsync(result.RefreshToken);
                    Authenticator.UpdateTenantId(result.Result.TenantId);

                    if (newResultEx.Result.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, we should copy tenant and user information from the cached token.
                        newResultEx.Result.UpdateTenantAndUserInfo(result.Result.TenantId, result.Result.IdToken, result.Result.UserInfo);
                    }
                }
                catch (AdalException ex)
                {
                    AdalServiceException serviceException = ex as AdalServiceException;
                    if (serviceException != null && serviceException.ErrorCode == "invalid_request")
                    {
                        throw new AdalServiceException(
                            AdalError.FailedToRefreshToken,
                            AdalErrorMessage.FailedToRefreshToken + ". " + serviceException.Message,
                            serviceException.ServiceErrorCodes,
                            serviceException);
                    }

                    newResultEx = new AuthenticationResultEx { Exception = ex };
                }
            }

            return newResultEx;
        }

        async Task<AuthenticationResultEx> SendHttpMessageAsync(IRequestParameters requestParameters)
        {
            var client = new AdalHttpClient(Authenticator.TokenUri, CallState) { Client = { BodyParameters = requestParameters } };
            TokenResponse tokenResponse = await client.GetResponseAsync<TokenResponse>(ClientMetricsEndpointType.Token);

            return tokenResponse.GetResult();
        }

        void NotifyBeforeAccessCache()
        {
            _tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs
            {
                TokenCache = _tokenCache,
                Resource = Resource,
                ClientId = ClientKey.ClientId,
                UniqueId = UniqueId,
                DisplayableId = DisplayableId
            });
        }

        void NotifyAfterAccessCache()
        {
            _tokenCache.OnAfterAccess(new TokenCacheNotificationArgs
            {
                TokenCache = _tokenCache,
                Resource = Resource,
                ClientId = ClientKey.ClientId,
                UniqueId = UniqueId,
                DisplayableId = DisplayableId
            });
        }

        void LogReturnedToken(AuthenticationResult result)
        {
            if (result.AccessToken != null)
            {
                string accessTokenHash = PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.AccessToken);

                PlatformPlugin.Logger.Information(CallState, string.Format(CultureInfo.CurrentCulture, "=== Token Acquisition finished successfully. An access token was retuned:\n\tAccess Token Hash: {0}\n\tExpiration Time: {1}\n\tUser Hash: {2}\n\t",
                    accessTokenHash,
                    result.ExpiresOn,                    
                    result.UserInfo != null ? PlatformPlugin.CryptographyHelper.CreateSha256Hash(result.UserInfo.UniqueId) : "null"));
            }
        }

        void ValidateAuthorityType()
        {
            if (!SupportADFS && Authenticator.AuthorityType == AuthorityType.ADFS)
            {
                throw new AdalException(AdalError.InvalidAuthorityType,
                    string.Format(CultureInfo.InvariantCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate, Authenticator.Authority));
            }
        }
    }
}
