// WorkspaceVersionSpec.cs
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

using System.Xml.Linq;

namespace MonoDevelop.VersionControl.TFS.Models
{
    sealed class WorkspaceVersionSpec : VersionSpec
    {
        private readonly string ownerName;

        public WorkspaceVersionSpec(string name, string ownerName)
        {
            this.Name = name;
            this.ownerName = ownerName;
        }

        public WorkspaceVersionSpec(WorkspaceData workspaceData)
        {
            this.Name = workspaceData.Name;
            this.ownerName = workspaceData.Owner;
        }

        internal override XElement ToXml(string elementName)
        {
            return new XElement(elementName,
                new XAttribute(XsiNs + "type", "WorkspaceVersionSpec"),
                new XAttribute("name", Name),
                new XAttribute("owner", OwnerName));
        }

        public string Name { get; set; }

        public string OwnerName { get { return ownerName; } }

        public override string DisplayString { get { return string.Format("W{0};{1}", Name, OwnerName); } }
    }
}