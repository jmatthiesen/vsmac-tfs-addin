// RenameDialog.cs
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

using System.IO;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using Xwt;

namespace MonoDevelop.VersionControl.TFS.Gui.Dialogs
{
	/// <summary>
    /// Rename dialog.
    /// </summary>
    public class RenameDialog : Dialog
    {
        ExtendedItem _item;
        TextEntry _nameEntry;

        internal RenameDialog(ExtendedItem item)
        {
            Init(item);
            BuildGui();
        }

        /// <summary>
        /// Gets the new path.
        /// </summary>
        /// <value>The new path.</value>
        public string NewPath
        {
            get
            {
                var dir = Path.GetDirectoryName(_item.LocalPath);

                return Path.Combine(dir, _nameEntry.Text);
            }
        }

        /// <summary>
		/// Init RenameDialog.
        /// </summary>
        /// <param name="item">Item.</param>
        void Init(ExtendedItem item)
        {
            _item = item;

			_nameEntry = new TextEntry
			{
				PlaceholderText = "New name"
			};
		}

        /// <summary>
        /// Builds the GUI.
        /// </summary>
        void BuildGui()
        {
            var content = new HBox();
            content.PackStart(new Label(GettextCatalog.GetString("New name") + ":"));
            _nameEntry.Text = _item.ServerPath.ItemName;
            _nameEntry.WidthRequest = 250;
            content.PackStart(_nameEntry);

            Buttons.Add(Command.Ok, Command.Cancel);
           
            Content = content;
            Resizable = false;
        }
    }
}