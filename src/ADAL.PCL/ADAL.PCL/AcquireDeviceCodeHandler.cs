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
    class AcquireDeviceCodeHandler
    {
        Authenticator _authenticator;
        ClientKey _clientKey;
        string _resource;
        CallState _callState;
        string _extraQueryParameters;

        public AcquireDeviceCodeHandler(Authenticator authenticator, string resource, string clientId, string extraQueryParameters)
        {
            _authenticator = authenticator;
            _callState = AcquireTokenHandlerBase.CreateCallState(authenticator.CorrelationId);
            _clientKey = new ClientKey(clientId);
            _resource = resource;
            _extraQueryParameters = extraQueryParameters;
        }
        
        string CreateDeviceCodeRequestUriString()
        {
            var deviceCodeRequestParameters = new DictionaryRequestParameters(_resource, _clientKey);

            if (_callState != null && _callState.CorrelationId != Guid.Empty)
            {
                deviceCodeRequestParameters[OAuthParameter.CorrelationId] = _callState.CorrelationId.ToString();
            }
            
            if (PlatformPlugin.HttpClientFactory.AddAdditionalHeaders)
            {
                IDictionary<string, string> adalIdParameters = AdalIdHelper.GetAdalIdParameters();
                foreach (KeyValuePair<string, string> kvp in adalIdParameters)
                {
                    deviceCodeRequestParameters[kvp.Key] = kvp.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(_extraQueryParameters))
            {
                // Checks for extraQueryParameters duplicating standard parameters
                Dictionary<string, string> kvps = EncodingHelper.ParseKeyValueList(_extraQueryParameters, '&', false, _callState);
                foreach (KeyValuePair<string, string> kvp in kvps)
                {
                    if (deviceCodeRequestParameters.ContainsKey(kvp.Key))
                    {
                        throw new AdalException(AdalError.DuplicateQueryParameter, string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DuplicateQueryParameterTemplate, kvp.Key));
                    }
                }

                deviceCodeRequestParameters.ExtraQueryParameter = _extraQueryParameters;
            }

            return new Uri(new Uri(_authenticator.DeviceCodeUri), "?" + deviceCodeRequestParameters).AbsoluteUri;
        }

        internal async Task<DeviceCodeResult> RunHandlerAsync()
        {
            await _authenticator.UpdateFromTemplateAsync(_callState);
            ValidateAuthorityType();
            AdalHttpClient client = new AdalHttpClient(CreateDeviceCodeRequestUriString(), _callState);
            DeviceCodeResponse response = await client.GetResponseAsync<DeviceCodeResponse>(ClientMetricsEndpointType.DeviceCode);

            if (!string.IsNullOrEmpty(response.Error))
            {
                throw new AdalException(response.Error, response.ErrorDescription);
            }

            return response.GetResult(_clientKey.ClientId, _resource);
        }

        void ValidateAuthorityType()
        {
            if (_authenticator.AuthorityType == AuthorityType.ADFS)
            {
                throw new AdalException(AdalError.InvalidAuthorityType,
                    string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.InvalidAuthorityTypeTemplate, _authenticator.Authority));
            }
        }

    }
}
