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
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Microsoft.IdentityService.Clients.ActiveDirectory
{
    class AdalHttpClient
    {
        const string DeviceAuthHeaderName = "x-ms-PKeyAuth";
        const string DeviceAuthHeaderValue = "1.0";
        const string WwwAuthenticateHeader = "WWW-Authenticate";
        const string PKeyAuthName = "PKeyAuth";

        public AdalHttpClient(string uri, CallState callState)
        {
            RequestUri = CheckForExtraQueryParameter(uri);
            Client = PlatformPlugin.HttpClientFactory.Create(RequestUri, callState);
            CallState = callState;
        }

        internal string RequestUri { get; set; }

        public IHttpClient Client { get; set; }

        public CallState CallState { get; set; }

        public async Task<T> GetResponseAsync<T>(string endpointType)
        {
            return await GetResponseAsync<T>(endpointType, true);
        }

        async Task<T> GetResponseAsync<T>(string endpointType, bool respondToDeviceAuthChallenge)
        {
            T typedResponse = default(T);
            IHttpWebResponse response;
            ClientMetrics clientMetrics = new ClientMetrics();

            try
            {
                clientMetrics.BeginClientMetricsRecord(CallState);

                if (PlatformPlugin.HttpClientFactory.AddAdditionalHeaders)
                {
                    Dictionary<string, string> clientMetricsHeaders = clientMetrics.GetPreviousRequestRecord(CallState);
                    foreach (KeyValuePair<string, string> kvp in clientMetricsHeaders)
                    {
                        Client.Headers[kvp.Key] = kvp.Value;
                    }

                    IDictionary<string, string> adalIdHeaders = AdalIdHelper.GetAdalIdParameters();
                    foreach (KeyValuePair<string, string> kvp in adalIdHeaders)
                    {
                        Client.Headers[kvp.Key] = kvp.Value;
                    }
                }

                //add pkeyauth header
                Client.Headers[DeviceAuthHeaderName] = DeviceAuthHeaderValue;
                using (response = await Client.GetResponseAsync())
                {
                    typedResponse = DeserializeResponse<T>(response.ResponseStream);
                    clientMetrics.SetLastError(null);
                }
            }
            catch (HttpRequestWrapperException ex)
            {
                if (!IsDeviceAuthChallenge(endpointType, ex.WebResponse, respondToDeviceAuthChallenge))
                {
                    AdalServiceException serviceEx;
                    if (ex.WebResponse != null)
                    {
                        TokenResponse tokenResponse = TokenResponse.CreateFromErrorResponse(ex.WebResponse);
                        string[] errorCodes = tokenResponse.ErrorCodes ?? new[] {ex.WebResponse.StatusCode.ToString()};
                        serviceEx = new AdalServiceException(tokenResponse.Error, tokenResponse.ErrorDescription,
                            errorCodes, ex);
                    }
                    else
                    {
                        serviceEx = new AdalServiceException(AdalError.Unknown, ex);
                    }

                    clientMetrics.SetLastError(serviceEx.ServiceErrorCodes);
                    PlatformPlugin.Logger.Error(CallState, serviceEx);
                    throw serviceEx;
                }
                else
                {
                    response = ex.WebResponse;
                }
            }
            finally
            {
                clientMetrics.EndClientMetricsRecord(endpointType, CallState);
            }

            // Check for pkeyauth challenge
            if (IsDeviceAuthChallenge(endpointType, response, respondToDeviceAuthChallenge))
            {
                return await HandleDeviceAuthChallenge<T>(endpointType, response);
            }

            return typedResponse;
        }

        bool IsDeviceAuthChallenge(string endpointType, IHttpWebResponse response, bool respondToDeviceAuthChallenge)
        {
            return PlatformPlugin.DeviceAuthHelper.CanHandleDeviceAuthChallenge &&
                   respondToDeviceAuthChallenge &&
                   (response.Headers.ContainsKey(WwwAuthenticateHeader) &&
                    response.Headers[WwwAuthenticateHeader].StartsWith(PKeyAuthName, StringComparison.CurrentCulture)) &&
                   endpointType.Equals(ClientMetricsEndpointType.Token);
        }

        IDictionary<string, string> ParseChallengeData(IHttpWebResponse response)
        {
            IDictionary<string, string> data = new Dictionary<string, string>();
            string wwwAuthenticate = response.Headers[WwwAuthenticateHeader];
            wwwAuthenticate = wwwAuthenticate.Substring(PKeyAuthName.Length + 1);
            List<string> headerPairs = EncodingHelper.SplitWithQuotes(wwwAuthenticate, ',');

            foreach (string pair in headerPairs)
            {
                List<string> keyValue = EncodingHelper.SplitWithQuotes(pair, '=');
                data.Add(keyValue[0].Trim(),keyValue[1].Trim().Replace("\"",""));
            }

            return data;
        }

        async Task<T> HandleDeviceAuthChallenge<T>(string endpointType, IHttpWebResponse response)
        {
            IDictionary<string, string> responseDictionary = ParseChallengeData(response);

            if (!responseDictionary.ContainsKey("SubmitUrl"))
            {
                responseDictionary["SubmitUrl"] = RequestUri;
            }

            string responseHeader = await PlatformPlugin.DeviceAuthHelper.CreateDeviceAuthChallengeResponse(responseDictionary);
            IRequestParameters rp = Client.BodyParameters;
            Client = PlatformPlugin.HttpClientFactory.Create(CheckForExtraQueryParameter(responseDictionary["SubmitUrl"]), CallState);
            Client.BodyParameters = rp;
            Client.Headers["Authorization"] = responseHeader;

            return await GetResponseAsync<T>(endpointType, false);
        }

        static T DeserializeResponse<T>(Stream responseStream)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));

            if (responseStream == null)
            {
                return default(T);
            }

            using (Stream stream = responseStream)
            {
                return ((T)serializer.ReadObject(stream));
            }
        }

        static string CheckForExtraQueryParameter(string url)
        {
            string extraQueryParameter = PlatformPlugin.PlatformInformation.GetEnvironmentVariable("ExtraQueryParameter");
            string delimiter = (url.IndexOf('?') > 0) ? "&" : "?";
          
            if (!string.IsNullOrWhiteSpace(extraQueryParameter))
            {
                url += string.Concat(delimiter, extraQueryParameter);
            }

            return url;
        }
    }
}