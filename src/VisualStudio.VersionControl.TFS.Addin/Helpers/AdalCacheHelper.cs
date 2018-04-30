// AdalCacheHelper.cs
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
using System.IO;
using Microsoft.IdentityService.Clients.ActiveDirectory;

namespace MonoDevelop.VersionControl.TFS.Helpers
{
	/// <summary>
    /// Adal cache helper.
    /// </summary>
    public static class AdalCacheHelper
    {
        static readonly string TokenCachePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        static readonly string TokenCacheExtension = ".txt";

        public static TokenCache GetAdalFileCacheInstance(string cacheId)
        {
            var cache = new TokenCache();

            LoadCacheFromFile(cacheId, cache);

            return cache;
        }

        /// <summary>
        /// Persists the OAuth token cache.
        /// </summary>
        /// <param name="cacheId">Cache identifier.</param>
        /// <param name="cache">Cache.</param>
        public static void PersistTokenCache(string cacheId, TokenCache cache)
        {
            PersistCacheToFile(cacheId, cache);
        }

        static void PersistCacheToFile(string cacheId, TokenCache cache)
        {
            var bytes = cache.Serialize();
            File.WriteAllBytes(Path.Combine(TokenCachePath, cacheId + TokenCacheExtension), bytes);
        }

        /// <summary>
        /// Loads an OAuth token from cache.
        /// </summary>
        /// <param name="cacheId">Cache identifier.</param>
        /// <param name="cache">Cache.</param>
        static void LoadCacheFromFile(string cacheId, TokenCache cache)
        {
            var file = Path.Combine(TokenCachePath, cacheId + TokenCacheExtension);

            if (File.Exists(file))
            {
                var str = File.ReadAllBytes(file);
                cache.Deserialize(str);
            }
        }
    }
}