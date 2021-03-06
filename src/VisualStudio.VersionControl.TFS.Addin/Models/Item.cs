// Item.cs
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
using MonoDevelop.VersionControl.TFS.Helpers;

namespace MonoDevelop.VersionControl.TFS.Models
{
    public sealed class Item: BaseItem
    {
        //<Item cs="1" date="2006-12-15T16:16:26.95Z" enc="-3" type="Folder" itemid="1" item="$/" />
        //<Item cs="30884" date="2012-08-29T15:35:18.273Z" enc="65001" type="File" itemid="189452" item="$/.gitignore" hash="/S3KuHKFNtrxTG7LeQA7LQ==" len="387" />
        internal static Item FromXml(XElement element)
        {
            if (element == null)
                return null;
            
            Item item = new Item();
            item.ServerItem = element.GetAttributeValue("item");
            item.ItemType = EnumHelper.ParseItemType(element.GetAttributeValue("type"));
            item.DeletionId = element.GetIntAttribute("did");
            item.CheckinDate = element.GetDateAttribute("date");
            item.ChangesetId = element.GetIntAttribute("cs");
            item.ItemId = element.GetIntAttribute("itemid");
            item.Encoding = element.GetIntAttribute("enc");

            if (!string.IsNullOrEmpty(element.GetAttributeValue("isbranch")))
            {
                item.IsBranch = element.GetBooleanAttribute("isbranch");
            }
            if (item.ItemType == ItemType.File)
            {
                item.ContentLength = element.GetIntAttribute("len");
                item.ArtifactUri = element.GetAttributeValue("durl");
                item.HashValue = element.GetByteArrayAttribute("hash");
            }
            return item;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Item instance ");
            sb.Append(GetHashCode());

            sb.Append("\n	 ItemId: ");
            sb.Append(ItemId);

            sb.Append("\n	 CheckinDate: ");
            sb.Append(CheckinDate.ToString("s"));

            sb.Append("\n	 ChangesetId: ");
            sb.Append(ChangesetId);

            sb.Append("\n	 DeletionId: ");
            sb.Append(DeletionId);

            sb.Append("\n	 ItemType: ");
            sb.Append(ItemType);

            sb.Append("\n	 ServerItem: ");
            sb.Append(ServerItem);

            sb.Append("\n	 ContentLength: ");
            sb.Append(ContentLength);

            sb.Append("\n	 Download URL: ");
            sb.Append(ArtifactUri);

            sb.Append("\n	 Hash: ");
            string hash = String.Empty;
            if (HashValue != null)
                hash = Convert.ToBase64String(HashValue);
            sb.Append(hash);

            return sb.ToString();
        }

        public int ContentLength { get; private set; }

        public DateTime CheckinDate { get; private set; }

        public int ChangesetId { get; private set; }

        public int DeletionId { get; private set; }

        public int Encoding { get; private set; }

        public int ItemId { get; private set; }

        public byte[] HashValue { get; private set; }

        public string ArtifactUri { get; private set; }

        public string ServerItem { get; private set; }

        public override RepositoryPath ServerPath { get { return new RepositoryPath(ServerItem, ItemType == ItemType.Folder); } }

        public string ShortName
        {
            get
            {
                return ServerPath.ItemName;
            }
        }

        public bool IsBranch { get; private set; }
    }
}