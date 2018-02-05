//
// Microsoft.TeamFoundation.VersionControl.Client.ItemTest
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
	public class ItemTest
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
		public void Item_DownloadFile()
		{
			// need TFS_ envvars for this test
			// this test also assumes the $TFS_PROJECT contains at least one file in 
			// the top level directory which is non-zero in length

			if (String.IsNullOrEmpty(tfsUrl)) return;
			TeamFoundationServer tfs = new TeamFoundationServer(tfsUrl, credentials);

			VersionControlServer vcs = (VersionControlServer) tfs.GetService(typeof(VersionControlServer));

			string itemPath = String.Format("$/{0}/*", Environment.GetEnvironmentVariable("TFS_PROJECT"));
			ItemSpec itemSpec = new ItemSpec(itemPath, RecursionType.OneLevel);

			ItemSet itemSet = vcs.GetItems(itemSpec, VersionSpec.Latest, 
																		 DeletedState.NonDeleted, ItemType.File, true);

			int i = 0;
			Item[] items = itemSet.Items;
			foreach (Item item in items)
				{
					if (item.ContentLength == 0) continue;
					i++;

					string fname = Path.GetTempFileName();
					item.DownloadFile(fname);

					FileInfo fileInfo = new FileInfo(fname);
					Assert.IsTrue(fileInfo.Length > 0);
					File.Delete(fname);

					// limit how many files we pull here
					if (i == 3) break;
				}
		}

	}
}

