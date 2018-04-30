﻿// VSTSAuthorizationConfig.cs
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
using MonoDevelop.VersionControl.TFS.Models;

namespace MonoDevelop.VersionControl.TFS.Gui.Widgets
{
	/// <summary>
    /// VSTS  Authorization config.
    /// </summary>
	sealed class VSTSAuthorizationConfig : TeamFoundationServerAuthorizationConfig, IOAuthAuthorizationConfig
    {
        public VSTSAuthorizationConfig()
        {
            Init();
        }

        /// <summary>
        /// Gets or sets the oauth token.
        /// </summary>
        /// <value>The oauth token.</value>
        public string OauthToken { get; set; }
       
        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        /// <value>The expires on.</value>
		public DateTimeOffset ExpiresOn { get; set; }

        void Init()
        {
            TFSContainer.Visible = false;
        }
    }
}