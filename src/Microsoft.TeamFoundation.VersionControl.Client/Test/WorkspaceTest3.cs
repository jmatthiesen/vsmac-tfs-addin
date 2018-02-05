//
// Microsoft.TeamFoundation.VersionControl.Client.WorkspaceTest3
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
	public class WorkspaceTest3
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

			workspace = versionControlServer.CreateWorkspace("WorkspaceTest3_Workspace", 
																											 Environment.GetEnvironmentVariable("TFS_USERNAME"),
																											 "My Comment", folders, Environment.MachineName);
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			workspace.Delete();
		}

		[Test]
		public void Workspace_GetServerItemForLocalItem_PassValidItem()
		{
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			string item1 = String.Format("{0}/foo.txt", serverItem);
			string item2 = workspace.GetServerItemForLocalItem(Path.Combine(Environment.CurrentDirectory, "foo.txt"));
			Assert.AreEqual(item1, item2);
		}

		[Test]
		public void Workspace_TryGetServerItemForLocalItem_PassValidItem()
		{
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			string item1 = String.Format("{0}/foo.txt", serverItem);
			string item2 = workspace.TryGetServerItemForLocalItem(Path.Combine(Environment.CurrentDirectory, "foo.txt"));
			Assert.AreEqual(item1, item2);
		}

		[Test]
		[ExpectedException(typeof(ItemNotMappedException))]
		public void Workspace_GetServerItemForLocalItem_PassInvalidItem()
		{
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			string item1 = String.Format("{0}/foo.txt", serverItem);
			string item2 = workspace.GetServerItemForLocalItem(item1);
			Assert.AreEqual(item1, item2);
		}

		[Test]
		public void Workspace_TryGetServerItemForLocalItem_PassInvalidItem()
		{
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			string item1 = String.Format("{0}/foo.txt", serverItem);
			string item2 = workspace.TryGetServerItemForLocalItem(item1);
			Assert.AreEqual(null, item2);
		}

		[Test]
		public void Workspace_GetLocalItemForServerItem_PassValidItem()
		{
			string serverItem = Environment.GetEnvironmentVariable("TFS_PROJECT");
			string item1 = Path.Combine(Environment.CurrentDirectory, "foo.txt");
			string item2 = workspace.GetLocalItemForServerItem(String.Format("$/{0}/{1}", serverItem, "foo.txt"));
			Assert.AreEqual(item1, item2);
		}

		[Test]
		public void Workspace_TryGetLocalItemForServerItem_PassValidItem()
		{
			string serverItem = Environment.GetEnvironmentVariable("TFS_PROJECT");
			string item1 = Path.Combine(Environment.CurrentDirectory, "foo.txt");
			string item2 = workspace.TryGetLocalItemForServerItem(String.Format("$/{0}/{1}", serverItem, "foo.txt"));
			Assert.AreEqual(item1, item2);
		}

		[Test]
		[ExpectedException(typeof(ItemNotMappedException))]
		public void Workspace_GetLocalItemForServerItem_PassInvalidItem()
		{
			string item1 = String.Format("$/{0}/foo.txt", Environment.CurrentDirectory);
			string item2 = workspace.GetLocalItemForServerItem(item1);
			Assert.AreEqual(item1, item2);
		}

		[Test]
		public void Workspace_TryGetLocalItemForServerItem_PassInvalidItem()
		{
			string item1 = String.Format("$/{0}/foo.txt", Environment.CurrentDirectory);
			string item2 = workspace.TryGetLocalItemForServerItem(item1);
			Assert.AreEqual(null, item2);
		}
  }
}

