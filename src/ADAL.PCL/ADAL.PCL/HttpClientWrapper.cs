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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityService.Clients.ActiveDirectory
{
    internal class HttpClientWrapper : IHttpClient
    {
        readonly string _uri;
        int timeoutInMilliSeconds = 30000;

        public HttpClientWrapper(string uri, CallState callState)
        {
            _uri = uri;
            Headers = new Dictionary<string, string>();
            CallState = callState;
        }

        protected CallState CallState { get; set; }

        public IRequestParameters BodyParameters { get; set; }

        public string Accept { get; set; }

        public string ContentType { get; set; }

        public bool UseDefaultCredentials { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public int TimeoutInMilliSeconds
        {
            set
            {
                timeoutInMilliSeconds = value;
            }
        }

        public async Task<IHttpWebResponse> GetResponseAsync()
        {
            using (HttpClient client = new HttpClient(HttpMessageHandlerFactory.GetMessageHandler(UseDefaultCredentials)))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                HttpRequestMessage requestMessage = new HttpRequestMessage();
                requestMessage.RequestUri = new Uri(_uri);
                requestMessage.Headers.Accept.Clear();

                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Accept ?? "application/json"));
            
                foreach (KeyValuePair<string, string> kvp in Headers)
                {
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                }

                bool addCorrelationId = (CallState != null && CallState.CorrelationId != Guid.Empty);
              
                if (addCorrelationId)
                {
                    requestMessage.Headers.Add(OAuthHeader.CorrelationId, CallState.CorrelationId.ToString());
                    requestMessage.Headers.Add(OAuthHeader.RequestCorrelationIdInResponse, "true");
                }

                client.Timeout = TimeSpan.FromMilliseconds(timeoutInMilliSeconds);

                HttpResponseMessage responseMessage;

                try
                {
                    if (BodyParameters != null)
                    {
                        HttpContent content;
                       
                        if (BodyParameters is StringRequestParameters)
                        {
                            content = new StringContent(BodyParameters.ToString(), Encoding.UTF8, ContentType);
                        }
                        else
                        {
                            content = new FormUrlEncodedContent(((DictionaryRequestParameters)BodyParameters).ToList());
                        }

                        requestMessage.Method = HttpMethod.Post;
                        requestMessage.Content = content;
                    }
                    else
                    {
                        requestMessage.Method = HttpMethod.Get;
                    }

                    responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);
                }
                catch (TaskCanceledException ex)
                {
                    throw new HttpRequestWrapperException(null, ex);
                }

                IHttpWebResponse webResponse = await CreateResponseAsync(responseMessage);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    try
                    {
                        throw new HttpRequestException(string.Format(CultureInfo.CurrentCulture, " Response status code does not indicate success: {0} ({1}).", (int)webResponse.StatusCode, webResponse.StatusCode));
                    }
                    catch (HttpRequestException ex)
                    {
                        webResponse.ResponseStream.Position = 0;
                        throw new HttpRequestWrapperException(webResponse, ex);
                    }
                }

                if (addCorrelationId)
                {
                    VerifyCorrelationIdHeaderInReponse(webResponse.Headers);
                }

                return webResponse;
            }
        }

        public async static Task<IHttpWebResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, string>();
          
            if (response.Headers != null)
            {
                foreach (var kvp in response.Headers)
                {
                    headers[kvp.Key] = kvp.Value.First();
                }
            }

            return new HttpWebResponseWrapper(await response.Content.ReadAsStreamAsync(), headers, response.StatusCode);
        }

        void VerifyCorrelationIdHeaderInReponse(Dictionary<string, string> headers)
        {
            foreach (string reponseHeaderKey in headers.Keys)
            {
                string trimmedKey = reponseHeaderKey.Trim();
                if (string.Compare(trimmedKey, OAuthHeader.CorrelationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string correlationIdHeader = headers[trimmedKey].Trim();
                    Guid correlationIdInResponse;
                   
                    if (!Guid.TryParse(correlationIdHeader, out correlationIdInResponse))
                    {
                        PlatformPlugin.Logger.Warning(CallState, string.Format(CultureInfo.CurrentCulture, "Returned correlation id '{0}' is not in GUID format.", correlationIdHeader));
                    }
                    else if (correlationIdInResponse != CallState.CorrelationId)
                    {
                        PlatformPlugin.Logger.Warning(
                            CallState,
                            string.Format(CultureInfo.CurrentCulture, "Returned correlation id '{0}' does not match the sent correlation id '{1}'", correlationIdHeader, CallState.CorrelationId));
                    }

                    break;
                }
            }
        }
    }
}