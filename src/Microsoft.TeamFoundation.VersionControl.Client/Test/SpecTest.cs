//
// Microsoft.TeamFoundation.VersionControl.Client.SpecTest
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
	public class SpecTest
	{
		[Test]
		public void LatestVersionSpec()
		{
			VersionSpec spec = VersionSpec.Latest;
			Assert.AreEqual("T", spec.DisplayString);
		}

		[Test]
		public void WorkspaceVersionSpec()
		{
			VersionSpec spec = new WorkspaceVersionSpec("name", "owner");
			Assert.AreEqual("Wname;owner", spec.DisplayString);
		}

		[Test]
		public void LabelVersionSpec()
		{
			VersionSpec spec = new LabelVersionSpec("label");
			Assert.AreEqual("Llabel", spec.DisplayString);
		}

		[Test]
		public void ChangesetVersionSpec()
		{
			VersionSpec spec = new ChangesetVersionSpec(1);
			Assert.AreEqual("C1", spec.DisplayString);
		}
	}
}

