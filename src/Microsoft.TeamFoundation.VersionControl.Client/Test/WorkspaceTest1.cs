//
// Microsoft.TeamFoundation.VersionControl.Client.WorkspaceTest1
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
	public class WorkspaceTest1
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
		public void Workspace_Get()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);
			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			WorkingFolder[] folders = new WorkingFolder[1];
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			folders[0] = new WorkingFolder(serverItem, Environment.CurrentDirectory);

			Workspace w1 = vcs.CreateWorkspace("CreateDelete1_Workspace", 
																				 Environment.GetEnvironmentVariable("TFS_USERNAME"),
																				 "My Comment", folders, Environment.MachineName);

			Workspace w2 = vcs.GetWorkspace("CreateDelete1_Workspace");
			Assert.AreEqual("My Comment", w2.Comment);

			w1.Delete();
		}

		[Test]
		public void Workspace_GetViaInfo()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);
			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			WorkingFolder[] folders = new WorkingFolder[1];
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			folders[0] = new WorkingFolder(serverItem, Environment.CurrentDirectory);

			Workspace w1 = vcs.CreateWorkspace("CreateDelete1_Workspace", 
																				 Environment.GetEnvironmentVariable("TFS_USERNAME"),
																				 "My Comment", folders, Environment.MachineName);

			//Workstation.Current.UpdateWorkspaceInfoCache(vcs, Environment.GetEnvironmentVariable("TFS_USERNAME"));
			
			WorkspaceInfo info = Workstation.Current.GetLocalWorkspaceInfo(Environment.CurrentDirectory);
			Workspace w2 = info.GetWorkspace(tfs);

			// does info.GetWorkspace talk to the server and get the
			// mapped paths or no? ANSWER: NO IT DOESN'T
			string serverItem2 = w2.TryGetServerItemForLocalItem("foo.txt");
			Assert.AreEqual(null, serverItem2);
			w1.Delete();
		}

		[Test]
		public void Workspace_RefreshMappings1()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);
			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			WorkingFolder[] folders = new WorkingFolder[1];
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			folders[0] = new WorkingFolder(serverItem, Environment.CurrentDirectory);

			Workspace w1 = vcs.CreateWorkspace("CreateDelete1_Workspace", 
																				 Environment.GetEnvironmentVariable("TFS_USERNAME"),
																				 "My Comment", folders, Environment.MachineName);

			//Workstation.Current.UpdateWorkspaceInfoCache(vcs, Environment.GetEnvironmentVariable("TFS_USERNAME"));
			
			WorkspaceInfo info = Workstation.Current.GetLocalWorkspaceInfo(Environment.CurrentDirectory);
			Workspace w2 = info.GetWorkspace(tfs);

			// this will talk to the server and get the mapped paths
			w2.RefreshMappings();

			string serverItem1 = String.Format("{0}/foo.txt", serverItem);
			string serverItem2 = w2.TryGetServerItemForLocalItem(Path.GetFullPath("foo.txt"));
			Assert.AreEqual(serverItem1, serverItem2);
			w1.Delete();
		}

		[Test]
		public void Workspace_RefreshMappings2()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);
			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			WorkingFolder[] folders = new WorkingFolder[1];
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			folders[0] = new WorkingFolder(serverItem, Environment.CurrentDirectory);

			Workspace w1 = vcs.CreateWorkspace("CreateDelete1_Workspace", 
																				 Environment.GetEnvironmentVariable("TFS_USERNAME"),
																				 "My Comment", folders, Environment.MachineName);

			//Workstation.Current.UpdateWorkspaceInfoCache(vcs, Environment.GetEnvironmentVariable("TFS_USERNAME"));
			
			WorkspaceInfo info = Workstation.Current.GetLocalWorkspaceInfo(Environment.CurrentDirectory);
			Workspace w2 = info.GetWorkspace(tfs);

			// this will talk to the server and get the mapped paths
			// BUT it will fail because we don't pass a full path like in RefreshMappings1
			w2.RefreshMappings();

			string serverItem2 = w2.TryGetServerItemForLocalItem("foo.txt");
			Assert.IsNull(serverItem2);
			w1.Delete();
		}

		[Test]
		public void Workspace_TryGetWorkingFolderForServerItem()
		{
			// need TFS_ envvars for this test
			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);
			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			WorkingFolder[] folders = new WorkingFolder[2];
			string serverItem = String.Format("$/{0}", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			folders[0] = new WorkingFolder(serverItem, Environment.CurrentDirectory);

			string deeper = Path.Combine(Environment.CurrentDirectory, "deeper");
			folders[1] = new WorkingFolder(serverItem + "/deeper", deeper);

			Workspace w1 = vcs.CreateWorkspace("CreateDelete1_Workspace", 
																				 Environment.GetEnvironmentVariable("TFS_USERNAME"),
																				 "My Comment", folders, Environment.MachineName);

			WorkspaceInfo info = Workstation.Current.GetLocalWorkspaceInfo(Environment.CurrentDirectory);
			Workspace w2 = info.GetWorkspace(tfs);

			// this will talk to the server and get the mapped paths
			w2.RefreshMappings();

			{
				string serverItem1 = String.Format("{0}/deeper/foo.txt", serverItem);
				WorkingFolder folder = w2.TryGetWorkingFolderForServerItem(serverItem1);
				Assert.AreEqual(deeper, folder.LocalItem);
			}

			{
				string serverItem1 = String.Format("junk/deeper/foo.txt", serverItem);
				WorkingFolder folder = w2.TryGetWorkingFolderForServerItem(serverItem1);
				Assert.IsNull(deeper);
			}

			w1.Delete();

		}

  }
}

