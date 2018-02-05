//
// Microsoft.TeamFoundation.VersionControl.Client.Workstation
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
using System.IO;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Microsoft.TeamFoundation.VersionControl.Client
{
  using NUnit.Framework;

	[TestFixture]
	public class WorkstationTest
	{
		private string tfsUrl;
		private ICredentials credentials;
		private Workspace workspace;
		private VersionControlServer versionControlServer;

		[TestFixtureSetUp] 
		public void FixtureSetUp()
		{
			tfsUrl = Environment.GetEnvironmentVariable("TFS_URL");
			if (String.IsNullOrEmpty(tfsUrl))
				{
					Console.WriteLine("Warning: Environment variable TFS_URL not set.");
					Console.WriteLine("					Some tests cannot be executed without TFS_URL.");
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

			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);
			versionControlServer = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			WorkingFolder[] folders = new WorkingFolder[1];
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			folders[0] = new WorkingFolder(serverItem, Environment.CurrentDirectory);

			workspace = versionControlServer.CreateWorkspace("UpdateWorkspaceInfoCache_Workspace", 
																				Environment.GetEnvironmentVariable("TFS_USERNAME"),
																				"My Comment", folders, Environment.MachineName);
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			workspace.Delete();
			// clear out test workspace from cache
			Workstation.Current.UpdateWorkspaceInfoCache(versionControlServer, Environment.GetEnvironmentVariable("TFS_USERNAME"));
		}

		[Test]
		public void Workstation_WorkspaceInfoCache()
		{
			Workstation.Current.UpdateWorkspaceInfoCache(versionControlServer, Environment.GetEnvironmentVariable("TFS_USERNAME"));
			WorkspaceInfo wInfo = Workstation.Current.GetLocalWorkspaceInfo(Environment.CurrentDirectory);
			Assert.IsNotNull(wInfo);
		}

  }
}

