// Change.cs
// 
// Authors:
//       Ventsislav Mladenov
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2018 Ventsislav Mladenov, Javier Suárez Ruiz
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

using System;
using System.Text;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.VersionControl.Enums;

namespace MonoDevelop.VersionControl.TFS.Models
{
    public sealed class Change
    {
        //          <Change type="None or Add or Edit or Encoding or Rename or Delete or Undelete or Branch or Merge or Lock or Rollback or SourceRename or Property" typeEx="int">
        //            <Item xsi:nil="true" />
        //            <MergeSources xsi:nil="true" />
        //          </Change>
        internal static Change FromXml(XElement element)
        {
            Change change = new Change();
            if (element.Attribute("type") != null && !string.IsNullOrEmpty(element.Attribute("type").Value))
            {
                change.ChangeType = (ChangeType)Enum.Parse(typeof(ChangeType), element.Attribute("type").Value.Replace(" ", ","), true);
            }

            change.Item = Item.FromXml(element.Element(element.Name.Namespace + "Item"));
           
            return change;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Change instance ");
            sb.Append(GetHashCode());

            sb.Append("\n	 ChangeType: ");
            sb.Append(ChangeType);

            sb.Append("\n	 Item: ");
            sb.Append(Item.ToString());

            return sb.ToString();
        }

        public ChangeType ChangeType { get; private set; }

        public Item Item { get; private set; }
    }
}