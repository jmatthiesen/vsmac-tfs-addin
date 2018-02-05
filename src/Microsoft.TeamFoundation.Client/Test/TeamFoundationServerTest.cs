//
// Microsoft.TeamFoundation.Client.TeamFoundationServer
//
// Authors:
//	Joel Reed (joelwreed@gmail.com)
//
// Copyright (C) 2007 Joel Reed
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;
using Microsoft.TeamFoundation.Client;

namespace Microsoft.TeamFoundation.Client
{
  using NUnit.Framework;

  [TestFixture]
  public class TeamFoundationServerTest
  {
		private string tfsUrl;
		private ICredentials credentials;

		[TestFixtureSetUp] 
		public void FixtureSetUp()
		{
			tfsUrl = Environment.GetEnvironmentVariable("TFS_URL");
			if (String.IsNullOrEmpty(tfsUrl))
				{
					Console.WriteLine("Warning: Environment variable TFS_URL not set.");
					Console.WriteLine("         Some tests cannot be executed without TFS_URL.");
					return;
				}

			string username = Environment.GetEnvironmentVariable("TFS_USERNAME");
			if (String.IsNullOrEmpty(username))
				{
					Console.WriteLine("Warning: No TFS user credentials specified.");
					return;
				}

			credentials = new NetworkCredential(username, 
																					Environment.GetEnvironmentVariable("TFS_PASSWORD"),
																					Environment.GetEnvironmentVariable("TFS_DOMAIN"));
		}

    [Test]
    public void UriProperty()
    {
			string url = "http://example.org:8080/";
			TeamFoundationServer tfs = new TeamFoundationServer(url);
      Assert.AreEqual("http://example.org:8080/", tfs.Uri.ToString());
		}

    [Test]
		public void NameProperty()
    {
			string url = "http://example.org:8080/";
			TeamFoundationServer tfs = new TeamFoundationServer(url);
      Assert.AreEqual("http://example.org:8080/", tfs.Name);
		}

    [Test]
		public void NamePropertyWithCredentials()
    {
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			// for some reason Name property works differently when you pass in credentials
			Uri uri = new Uri(tfsUrl);
      Assert.AreEqual(uri.Host, tfs.Name);
		}

		[Test]
		public void Authentication1()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);
			Assert.IsFalse(tfs.HasAuthenticated);
		}
  }
}

