// ItemSetToHierarchItemConverter.cs
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
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.VersionControl.TFS.Models
{

    internal static class ItemSetToHierarchItemConverter
    {
        public static HierarchyItem Convert(List<Item> items)
        {
            HierarchyItem[] linerHierarchy = items.Select(x => new HierarchyItem(x)).ToArray();
            HierarchyItem root = linerHierarchy[0];

            for (int i = 1; i < linerHierarchy.Length; i++)
            {
                var currentLine = linerHierarchy[i];
                for (int j = i - 1; j >= 0; j--)
                {
                    var previousLine = string.Equals(linerHierarchy[j].ServerPath, RepositoryPath.RootPath ) ?
                                                     RepositoryPath.RootPath : linerHierarchy[j].ServerPath + RepositoryPath.Separator;
                    if (currentLine.ServerPath.StartsWith(previousLine, StringComparison.Ordinal) &&
                        currentLine.ServerPath.Substring(previousLine.Length).IndexOf(RepositoryPath.Separator) == -1)
                    {
                        currentLine.Parent = linerHierarchy[j];
                        currentLine.Parent.Children.Add(currentLine);
                        break;
                    }
                }
            }

            return root;
        }
    }
}
