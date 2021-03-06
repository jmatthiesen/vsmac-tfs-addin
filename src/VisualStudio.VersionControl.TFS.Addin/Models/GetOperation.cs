// GetOperation.cs
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

using System.Text;
using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Helpers;

namespace MonoDevelop.VersionControl.TFS.Models
{
    public class GetOperation
    {
        //<s:complexType name="GetOperation">
        //    <s:sequence>
        //        <s:element minOccurs="0" maxOccurs="1" name="HashValue" type="s:base64Binary"/>
        //        <s:element minOccurs="0" maxOccurs="1" name="Properties" type="tns:ArrayOfPropertyValue"/>
        //        <s:element minOccurs="0" maxOccurs="1" name="PropertyValues" type="tns:ArrayOfPropertyValue"/>
        //    </s:sequence>
        //    <s:attribute default="Any" name="type" type="tns:ItemType"/>
        //    <s:attribute default="0" name="itemid" type="s:int"/>
        //    <s:attribute name="slocal" type="s:string"/>
        //    <s:attribute name="tlocal" type="s:string"/>
        //    <s:attribute name="titem" type="s:string"/>
        //    <s:attribute name="sitem" type="s:string"/>
        //    <s:attribute default="0" name="sver" type="s:int"/>
        //    <s:attribute default="-2" name="vrevto" type="s:int"/>
        //    <s:attribute default="0" name="lver" type="s:int"/>
        //    <s:attribute default="0" name="did" type="s:int"/>
        //    <s:attribute default="0" name="chgEx" type="s:int"/>
        //    <s:attribute default="None" name="chg" type="tns:ChangeType"/>
        //    <s:attribute default="None" name="lock" type="tns:LockLevel"/>
        //    <s:attribute default="true" name="il" type="s:boolean"/>
        //    <s:attribute default="0" name="pcid" type="s:int"/>
        //    <s:attribute default="false" name="cnflct" type="s:boolean"/>
        //    <s:attribute default="None" name="cnflctchg" type="tns:ChangeType"/>
        //    <s:attribute default="0" name="cnflctchgEx" type="s:int"/>
        //    <s:attribute default="0" name="cnflctitemid" type="s:int"/>
        //    <s:attribute name="nmscnflct" type="s:unsignedByte" use="required"/>
        //    <s:attribute name="durl" type="s:string"/>
        //    <s:attribute default="-2" name="enc" type="s:int"/>
        //    <s:attribute default="0001-01-01T00:00:00" name="vsd" type="s:dateTime"/>
        //</s:complexType>
        internal static GetOperation FromXml(XElement element)
        {
            GetOperation getOperation = new GetOperation
            {
                ChangeType = ChangeType.None,
                ItemType = ItemType.Any,
                LockLevel = LockLevel.None,
                VersionServer = 0,
                VersionLocal = 0,
            };

            getOperation.ItemType = EnumHelper.ParseItemType(element.GetAttributeValue("type"));
            getOperation.ItemId = element.GetIntAttribute("itemid");
            getOperation.SourceLocalItem = element.GetAttributeValue("slocal");
            getOperation.TargetLocalItem = element.GetAttributeValue("tlocal");
            RepositoryPath sourceSeverItem;
            if (RepositoryPath.TryGet(element.GetAttributeValue("sitem"), getOperation.ItemType == ItemType.Folder, out sourceSeverItem))
            {
                getOperation.SourceServerItem = sourceSeverItem;
            }
            RepositoryPath targetServerItem;
            if (RepositoryPath.TryGet(element.GetAttributeValue("titem"), getOperation.ItemType == ItemType.Folder, out targetServerItem))
            {
                getOperation.TargetServerItem = targetServerItem;
            }
            getOperation.VersionServer = element.GetIntAttribute("sver");
            getOperation.VersionLocal = element.GetIntAttribute("lver");
            getOperation.ChangeType = EnumHelper.ParseChangeType(element.GetAttributeValue("chg"));

            // setup download url if found
            getOperation.ArtifactUri = element.GetAttributeValue("durl");

            // here's what you get if you remap a working folder from one
            // team project to another team project with the same file
            // first you get the update getOperation, then you get this later on
            // <GetOperation type="File" itemid="159025" slocal="foo.xml" titem="$/bar/foo.xml" lver="12002"><HashValue /></GetOperation>

            // look for a deletion id
            getOperation.DeletionId = element.GetIntAttribute("did");
            return getOperation;
        }

        public ChangeType ChangeType { get; private set; }

        public int DeletionId { get; private set; }

        public int ItemId { get; private set; }

        public ItemType ItemType { get; private set; }

        public LocalPath TargetLocalItem { get; private set; }

        public LocalPath SourceLocalItem { get; private set; }

        public RepositoryPath SourceServerItem { get; private set; }

        public RepositoryPath TargetServerItem { get; private set; }

        public int VersionLocal { get; private set; }

        public int VersionServer { get; private set; }

        public string ArtifactUri { get; private set; }

        public LockLevel LockLevel { get; private set; }

        public bool IsAdd
        {
            get { return ChangeType.HasFlag(ChangeType.Add); }
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

        public bool IsRename
        {
            get { return ChangeType.HasFlag(ChangeType.Rename); }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n	 type: ");
            sb.Append(ItemType.ToString());

            sb.Append("\n	 itemid: ");
            sb.Append(ItemId);

            sb.Append("\n	 slocal: ");
            sb.Append(SourceLocalItem);

            sb.Append("\n	 tlocal: ");
            sb.Append(TargetLocalItem);

            sb.Append("\n	 titem: ");
            sb.Append(TargetServerItem);

            sb.Append("\n	 sver: ");
            sb.Append(VersionServer);

            sb.Append("\n	 lver: ");
            sb.Append(VersionLocal);

            sb.Append("\n	 did: ");
            sb.Append(DeletionId);

            sb.Append("\n	 ArtifactUri: ");
            sb.Append(ArtifactUri);

            sb.Append("\n	 ChangeType: ");
            sb.Append(ChangeType.ToString());

            return sb.ToString();
        }
    }
}