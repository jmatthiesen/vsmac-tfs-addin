//
// Microsoft.TeamFoundation.Client.CommonStructureServiceTest
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
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

namespace Microsoft.TeamFoundation.Client
{
  using NUnit.Framework;

  [TestFixture]
  public class CommonStructureServiceTest
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
		public void GetService_CommonStructureService()
    {
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			ICommonStructureService css = (ICommonStructureService) tfs.GetService(typeof(ICommonStructureService));
      Assert.IsNotNull(css);
		}

    [Test]
		public void ListProjects()
    {
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			ICommonStructureService css = (ICommonStructureService) tfs.GetService(typeof(ICommonStructureService));
			ProjectInfo[] projects = css.ListProjects();
			
			foreach (ProjectInfo pinfo in projects)
				{
					Assert.IsNotNull(pinfo.Name);
				}
		}

    [Test]
		public void ListAllProjects()
    {
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			ICommonStructureService css = (ICommonStructureService) tfs.GetService(typeof(ICommonStructureService));
			ProjectInfo[] projects = css.ListAllProjects();
			
			foreach (ProjectInfo pinfo in projects)
				{
					Assert.IsNotNull(pinfo.Name);
					Assert.IsNotNull(pinfo.Status);
					Assert.IsNotNull(pinfo.Uri);
				}
		}

    [Test]
		public void GetProjectFromName()
    {
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			ICommonStructureService css = (ICommonStructureService) tfs.GetService(typeof(ICommonStructureService));
			ProjectInfo pinfo = css.GetProjectFromName(Environment.GetEnvironmentVariable("TFS_PROJECT"));
			
			Assert.IsNotNull(pinfo.Name);
			Assert.IsNotNull(pinfo.Status);
			Assert.IsNotNull(pinfo.Uri);
		}

    [Test]
		public void GetProject()
    {
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			ICommonStructureService css = (ICommonStructureService) tfs.GetService(typeof(ICommonStructureService));
			ProjectInfo p1 = css.GetProjectFromName(Environment.GetEnvironmentVariable("TFS_PROJECT"));
			ProjectInfo p2 = css.GetProject(p1.Uri);

			Assert.IsNotNull(p2.Name);
			Assert.IsNotNull(p2.Status);
			Assert.IsNotNull(p2.Uri);
		}

    [Test]
		public void GetProjectProperties()
    {
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			ICommonStructureService css = (ICommonStructureService) tfs.GetService(typeof(ICommonStructureService));
			ProjectInfo p1 = css.GetProjectFromName(Environment.GetEnvironmentVariable("TFS_PROJECT"));

			string projectName = "";
			string state = "";
			int templateId = 0;
			ProjectProperty[] properties = null;

			css.GetProjectProperties(p1.Uri, out projectName, out state, out templateId, out properties);
			Assert.IsNotNull(projectName);
		}

  }
}

