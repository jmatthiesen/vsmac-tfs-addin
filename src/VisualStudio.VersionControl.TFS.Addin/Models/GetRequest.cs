// GetRequest.cs
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
    public sealed class GetRequest
    {
        public GetRequest(BasePath item, RecursionType recursionType, VersionSpec versionSpec)
        {
            ItemSpec = new ItemSpec(item, recursionType);
            VersionSpec = versionSpec;
        }

        public GetRequest(ItemSpec itemSpec, VersionSpec versionSpec)
        {
            ItemSpec = itemSpec;
            VersionSpec = versionSpec;
        }

        internal GetRequest(VersionSpec versionSpec)
        {
            VersionSpec = versionSpec;
        }

        public ItemSpec ItemSpec { get; private set; }

        public VersionSpec VersionSpec { get; private set; }

        internal XElement ToXml()
        {
            XElement result = new XElement("GetRequest");
        
            if (ItemSpec != null)
                result.Add(ItemSpec.ToXml("ItemSpec"));
            
            result.Add(VersionSpec.ToXml("VersionSpec"));
         
            return result;
        }

        public override string ToString()
        {
            return string.Format("[GetRequest: ItemSpec={0}, VersionSpec={1}]", ItemSpec, VersionSpec);
        }
    }
}