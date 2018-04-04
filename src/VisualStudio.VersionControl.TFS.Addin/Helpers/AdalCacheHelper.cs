// CollectionHelper.cs
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
using System.Text;
using Microsoft.IdentityService.Clients.ActiveDirectory;

namespace MonoDevelop.VersionControl.TFS.Helpers
{
    public class AdalCacheHelper
    {
        static readonly string TokenCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MonoDevelop.VersionControl.TFS.TokenCache.txt");

        public static TokenCache GetAdalFileCacheInstance()
        {
            var cache = new TokenCache();

            LoadCacheFromFile(cache);

            return cache;
        }

        public static void PersistTokenCache(TokenCache cache)
        {
            PersistCacheToFile(cache);
        }

        static void PersistCacheToFile(TokenCache cache)
        {
            var bytes = cache.Serialize();
            File.WriteAllBytes(TokenCachePath, bytes);
        }

        static void LoadCacheFromFile(TokenCache cache)
        {
            var file = TokenCachePath;

            if (File.Exists(file))
            {
                var str = File.ReadAllBytes(file);
                cache.Deserialize(str);
            }
        }
    }
}