// TeamFoundationServerCommitDialogExtension.cs
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

using MonoDevelop.VersionControl.TFS.Gui.Widgets;

namespace MonoDevelop.VersionControl.TFS.Extensions
{
	/// <summary>
    /// Team Foundation Server commit dialog extension.
    /// </summary>
	public class TeamFoundationServerCommitDialogExtension : CommitDialogExtension
    {
		TeamFoundationServerCommitDialogExtensionWidget _widget;

        public override bool Initialize(ChangeSet changeSet)
        {
			if (changeSet.Repository is TeamFoundationServerRepository repo)
			{
				_widget = new TeamFoundationServerCommitDialogExtensionWidget();
				Add(_widget);

				_widget.Show();
				Show();

				return true;
			}

			return false;
		}

        /// <summary>
        /// Begin commit.
        /// </summary>
        /// <returns><c>true</c>, if begin commit was oned, <c>false</c> otherwise.</returns>
        /// <param name="changeSet">Change set.</param>
        public override bool OnBeginCommit(ChangeSet changeSet)
        {
			changeSet.ExtendedProperties["TFS.WorkItems"] = _widget.WorkItems;
           
			return true;
        }

        /// <summary>
        /// End commit.
        /// </summary>
        /// <param name="changeSet">Change set.</param>
        /// <param name="success">If set to <c>true</c> success.</param>
        public override void OnEndCommit(ChangeSet changeSet, bool success)
        {
            base.OnEndCommit(changeSet, success);
        }
    }
}