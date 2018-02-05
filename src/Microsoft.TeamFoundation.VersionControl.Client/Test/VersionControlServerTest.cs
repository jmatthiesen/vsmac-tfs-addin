//
// Microsoft.TeamFoundation.VersionControl.Client.VersionControlServerTest
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
	public class VersionControlServerTest
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
		}

		[Test]
		public void GetService_VersionControlServer()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));
			Assert.IsNotNull(vcs);
		}

		[Test]
		public void Workspace_CreateDelete1()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));
			Workspace w1 = vcs.CreateWorkspace("CreateDelete1_Workspace", 
																				 Environment.GetEnvironmentVariable("TFS_USERNAME"));

			w1.Delete();
		}

		[Test]
		public void Workspace_CreateDelete2()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			WorkingFolder[] folders = new WorkingFolder[1];
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			folders[0] = new WorkingFolder(serverItem, 
																		 Environment.CurrentDirectory);

			Workspace w1 = vcs.CreateWorkspace("CreateDelete2_Worspace", 
																				 Environment.GetEnvironmentVariable("TFS_USERNAME"),
																				 "CreateDelete2 Comment",
																				 folders, "CreateDelete2_Computer");

			w1.Delete();
		}

		[Test]
		public void Workspace_GetItems1()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			string itemPath = String.Format("$/{0}/*", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			ItemSpec itemSpec = new ItemSpec(itemPath, RecursionType.OneLevel);

			ItemSet itemSet = vcs.GetItems(itemSpec, VersionSpec.Latest, 
																		 DeletedState.NonDeleted, ItemType.Any, true);

			Item[] items = itemSet.Items;
			foreach (Item item in items)
				{
					Assert.IsNotNull(item.ServerItem);
				}
		}

		[Test]
		public void Workspace_GetItems2()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));
			string itemPath = String.Format("$/{0}/*", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			ItemSet itemSet = vcs.GetItems(itemPath, RecursionType.OneLevel);

			Item[] items = itemSet.Items;
			foreach (Item item in items)
				{
					Assert.IsNotNull(item.ServerItem);
				}
		}

		[Test]
		public void Workspace_GetItems3()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));
			string itemPath = String.Format("$/{0}/*", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			ItemSet itemSet = vcs.GetItems(itemPath, RecursionType.OneLevel);

			Item[] items = itemSet.Items;
			foreach (Item item in items)
				{
					Item x = vcs.GetItem(item.ItemId, 1);
					Assert.AreEqual(x.ItemId, item.ItemId);
					Assert.IsNotNull(x.ArtifactUri);
				}
		}

		[Test]
		public void Workspace_QueryLabels1()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			string path = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			VersionControlLabel[] labels = vcs.QueryLabels(null, path, null, false);

			foreach (VersionControlLabel label in labels)
				{
					Assert.IsNotNull(label.Name);
				}
		}

		[Test]
		public void Workspace_QueryLabels2()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			string path = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			VersionControlLabel[] labels = vcs.QueryLabels(null, path, null, false,
																										 null, null);

			foreach (VersionControlLabel label in labels)
				{
					Assert.IsNotNull(label.Name);
				}
		}

		[Test]
		public void Workspace_QueryLabels3()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			string path = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			VersionControlLabel[] labels = vcs.QueryLabels(null, path, null, false,
																										 null, null, true);

			foreach (VersionControlLabel label in labels)
				{
					Assert.IsNotNull(label.Name);
					Assert.IsNotNull(label.ArtifactUri);
				}
		}

		[Test]
		public void Workspace_QueryWorkspaces1()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			Workspace[] workspaces = vcs.QueryWorkspaces(null, null, null);
			foreach (Workspace workspace in workspaces)
				{
					Assert.IsNotNull(workspace.Name);
				}
		}

		[Test]
		public void Workspace_QueryWorkspaces2()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			Workspace[] workspaces = vcs.QueryWorkspaces(null, null, Environment.MachineName);
			foreach (Workspace workspace in workspaces)
				{
					Assert.IsNotNull(workspace.Name);
				}
		}

		[Test]
		public void Workspace_QueryWorkspaces3()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			Workspace[] workspaces = vcs.QueryWorkspaces(null, Environment.GetEnvironmentVariable("TFS_USERNAME"), 
																									 Environment.MachineName);
			foreach (Workspace workspace in workspaces)
				{
					Assert.IsNotNull(workspace.Name);
				}
		}

	}
}

