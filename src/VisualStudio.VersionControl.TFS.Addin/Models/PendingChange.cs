// PendingChange.cs
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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Helpers;

namespace MonoDevelop.VersionControl.TFS.Models
{
    //<s:complexType name="PendingChange">
    //    <s:sequence>
    //        <s:element minOccurs="0" maxOccurs="1" name="MergeSources" type="tns:ArrayOfMergeSource"/>
    //        <s:element minOccurs="0" maxOccurs="1" name="PropertyValues" type="tns:ArrayOfPropertyValue"/>
    //    </s:sequence>
    //    <s:attribute default="0" name="chgEx" type="s:int"/>
    //    <s:attribute default="None" name="chg" type="tns:ChangeType"/>
    //    <s:attribute name="date" type="s:dateTime" use="required"/>
    //    <s:attribute default="0" name="did" type="s:int"/>
    //    <s:attribute default="Any" name="type" type="tns:ItemType"/>
    //    <s:attribute default="-2" name="enc" type="s:int"/>
    //    <s:attribute default="0" name="itemid" type="s:int"/>
    //    <s:attribute name="local" type="s:string"/>
    //    <s:attribute default="None" name="lock" type="tns:LockLevel"/>
    //    <s:attribute name="item" type="s:string"/>
    //    <s:attribute name="srclocal" type="s:string"/>
    //    <s:attribute name="srcitem" type="s:string"/>
    //    <s:attribute default="0" name="svrfm" type="s:int"/>
    //    <s:attribute default="0" name="sdi" type="s:int"/>
    //    <s:attribute default="0" name="ver" type="s:int"/>
    //    <s:attribute name="hash" type="s:base64Binary"/>
    //    <s:attribute default="-1" name="len" type="s:long"/>
    //    <s:attribute name="uhash" type="s:base64Binary"/>
    //    <s:attribute default="0" name="pcid" type="s:int"/>
    //    <s:attribute name="durl" type="s:string"/>
    //    <s:attribute name="shelvedurl" type="s:string"/>
    //    <s:attribute name="ct" type="s:int" use="required"/>
    //</s:complexType>
    public sealed class PendingChange
    {
        internal static PendingChange FromXml(XElement element)
        {
            PendingChange change = new PendingChange();
            change.LocalItem = element.GetAttributeValue("local");
            change.ItemId = element.GetIntAttribute("itemid");
            change.Encoding = element.GetIntAttribute("enc");
            change.Version = element.GetIntAttribute("ver");
            change.CreationDate = element.GetDateAttribute("date");
            change.Hash = element.GetByteArrayAttribute("hash");
            change.uploadHashValue = element.GetByteArrayAttribute("uhash");
            change.ItemType = EnumHelper.ParseItemType(element.GetAttributeValue("type"));
            change.DownloadUrl = element.GetAttributeValue("durl");
            change.ChangeType = EnumHelper.ParseChangeType(element.GetAttributeValue("chg"));

            if (change.ChangeType == ChangeType.Edit)
                change.ItemType = ItemType.File;

            change.ServerItem = new RepositoryPath(element.GetAttributeValue("item"), change.ItemType == ItemType.Folder);

            return change;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("PendingChange instance ");
            sb.Append(GetHashCode());

            sb.Append("\n	 ServerItem: ");
            sb.Append(ServerItem);

            sb.Append("\n	 LocalItem: ");
            sb.Append(LocalItem);

            sb.Append("\n	 ItemId: ");
            sb.Append(ItemId);

            sb.Append("\n	 Encoding: ");
            sb.Append(Encoding);

            sb.Append("\n	 Creation Date: ");
            sb.Append(CreationDate);

            sb.Append("\n	 ChangeType: ");
            sb.Append(ChangeType);

            sb.Append("\n	 ItemType: ");
            sb.Append(ItemType);

            sb.Append("\n	 Download URL: ");
            sb.Append(DownloadUrl);

            return sb.ToString();
        }

        internal void UpdateUploadHashValue()
        {
            using (FileStream stream = new FileStream(LocalItem, FileMode.Open, FileAccess.Read))
            {
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                md5.ComputeHash(stream);
                uploadHashValue = md5.Hash;
            }
        }

        public byte[] Hash { get; set; }

        byte[] uploadHashValue;

        public byte[] UploadHashValue
        {
            get
            {
                if (uploadHashValue == null)
                    UpdateUploadHashValue();
                return uploadHashValue; 
            }
        }

        public DateTime CreationDate { get; private set; }

        public int Encoding { get; private set; }

        public LocalPath LocalItem { get; private set; }

        public int ItemId { get; private set; }

        public ItemType ItemType { get; private set; }

        public int Version { get; private set; }

        public bool IsAdd
        {
            get { return ChangeType.HasFlag(ChangeType.Add); }
        }

        public bool IsBranch
        {
            get { return ChangeType.HasFlag(ChangeType.Branch); }
        }

        public bool IsDelete
        {
            get { return ChangeType.HasFlag(ChangeType.Delete); }
        }

        public bool IsEdit
        {
            get { return ChangeType.HasFlag(ChangeType.Edit); }
        }

        public bool IsEncoding
        {
            get { return ChangeType.HasFlag(ChangeType.Encoding); }
        }

        public bool IsLock
        {
            get { return ChangeType.HasFlag(ChangeType.Lock); }
        }

        public bool IsMerge
        {
            get { return ChangeType.HasFlag(ChangeType.Merge); }
        }

        public bool IsRename
        {
            get { return ChangeType.HasFlag(ChangeType.Rename); }
        }

        public ChangeType ChangeType { get; private set; }

        public RepositoryPath ServerItem { get; private set; }

        public string DownloadUrl { get; set; }

        static public string GetLocalizedStringForChangeType(ChangeType changeType)
        {
            return changeType.ToString();
        }
    }
}