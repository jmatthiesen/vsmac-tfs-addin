// ItemSpec.cs
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
using System.Xml.Linq;

namespace MonoDevelop.VersionControl.TFS.Models
{
    public sealed class ItemSpec
    {
        public ItemSpec(string item, RecursionType recursionType)
        {
            if (string.IsNullOrEmpty(item))
                throw new ArgumentException("Value cannot be null or empty.");

            Item = item;
            RecursionType = recursionType;
        }

        public ItemSpec(string item, RecursionType recursionType, int deletionId)
        {
            if (string.IsNullOrEmpty(item))
                throw new ArgumentException("Value cannot be null or empty.");

            Item = item;
            RecursionType = recursionType;
            DeletionId = deletionId;
        }

        internal static ItemSpec FromServerItem(BaseItem item)
        {
            return new ItemSpec(item.ServerPath, item.ItemType == ItemType.Folder ? RecursionType.Full : RecursionType.None);
        }

        internal static ItemSpec FromLocalPath(LocalPath path)
        {
            return new ItemSpec(path, path.IsDirectory ? RecursionType.Full : RecursionType.None);
        }

        internal static ItemSpec FromServerPath(RepositoryPath path)
        {
            return new ItemSpec(path, path.IsDirectory ? RecursionType.Full : RecursionType.None);
        }

        internal XElement ToXml(string elementName)
        {
            XElement result = new XElement(elementName);
          
            if (RecursionType != RecursionType.None)
                result.Add(new XAttribute("recurse", RecursionType));
           
            if (DeletionId != 0)
                result.Add(new XAttribute("did", DeletionId));
           
            if (RepositoryPath.IsServerItem(Item))
                result.Add(new XAttribute("item", Item));
            else
                result.Add(new XAttribute("item", (new LocalPath(Item)).ToRepositoryLocalPath()));
           
            return result;
        }

        public int DeletionId { get; set; }

        public string Item { get; set; }

        public RecursionType RecursionType { get; set; }

        public override string ToString()
        {
            return string.Format("Item: {0}, Recursion: {1}", Item, RecursionType);
        }
    }
}