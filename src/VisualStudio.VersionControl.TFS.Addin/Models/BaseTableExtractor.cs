// BaseTableExtractor.cs
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

using System.Linq;
using System.Xml.Linq;

namespace MonoDevelop.VersionControl.TFS.Models
{
    class BaseTableExtractor
    {
        protected readonly XNamespace ns = "http://schemas.microsoft.com/TeamFoundation/2005/06/WorkItemTracking/ClientServices/03";
     
        protected readonly XElement _response;
        protected readonly string _tableName;

        public BaseTableExtractor(XElement response, string tableName)
        {
            _tableName = tableName;
            _response = response;
        }

        protected XElement GetTable()
        {
            var tables = _response.Descendants(ns + "table");
            return tables.FirstOrDefault(t => string.Equals(t.Attribute("name").Value, _tableName, System.StringComparison.OrdinalIgnoreCase));
        }

        protected XElement[] GetColumns(XElement table)
        {
            return table.Element(ns + "columns").Elements(ns + "c").ToArray();
        }
    } 
}