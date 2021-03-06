// StoredQuery.cs
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
using System.Text;
using System.Xml.Linq;

namespace MonoDevelop.VersionControl.TFS.Models
{
	public sealed class StoredQuery
    {
        [TableFieldName("ID")]
        public Guid Id { get; set; }

        [TableFieldName("ProjectID")]
        public int ProjectId { get; set; }

        [TableFieldName("fPublic")]
        public bool IsPublic { get; set; }

        [TableFieldName("Owner")]
        public string Owner { get; set; }

        [TableFieldName("QueryName")]
        public string QueryName { get; set; }

        [TableFieldName("QueryText")]
        public string QueryText { get; set; }

        [TableFieldName("Description")]
        public string Description { get; set; }

        [TableFieldName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [TableFieldName("LastWriteTime")]
        public DateTime LastWriteTime { get; set; }

        [TableFieldName("fDeleted")]
        public bool IsDeleted { get; set; }

        [TableFieldName("ParentID")]
        public Guid ParentId { get; set; }

		public XElement GetWiqlXml(XNamespace messageNs, string url, string projectName)
		{          
			XElement wiqlNode = new XElement("Wiql", QueryText);

			XElement dayPrecissionNode = new XElement("DayPrecision", true);
                     
			XElement projectNode = new XElement("Context", QueryText);
			projectNode.Add(new XAttribute("Key", "project"));
			projectNode.Add(new XAttribute("Value", projectName));
			projectNode.Add(new XAttribute("ValueType", "String"));
		
			XElement teamNode = new XElement("Context", QueryText);
			teamNode.Add(new XAttribute("Key", "team"));
			teamNode.Add(new XAttribute("Value", string.Format("{0} Team", projectName)));
			teamNode.Add(new XAttribute("ValueType", "String"));
         
			XNamespace queryNs = XNamespace.Get("");

			XElement element =  
				new XElement(messageNs + "psQuery", 
				             new XElement(queryNs + "Query", 
				                          new XAttribute("Product", url),
				                          wiqlNode,  dayPrecissionNode, projectNode, teamNode));
			return element;
		}

        public XElement GetQueryXml(WorkItemContext context, FieldList fields)
        {
            var parser = new LexalParser(QueryText);
            var nodes = parser.ProcessWherePart();
            nodes.Optimize();
            nodes.ExtractOperatorForward();
            nodes.FixFields(fields);
            nodes.FillFieldTypes(fields);

            var manager = new ParameterManager(context);
            manager.EvalParameters(nodes);

            var xmlTransformer = new NodesToXml(nodes);
         
            return XElement.Parse(xmlTransformer.WriteXml());
        }

        public XElement GetSortingXml()
        {
            var parser = new LexalParser(QueryText);
            var sortList = parser.ProcessOrderBy();
        
            return XElement.Parse(sortList.WriteToXml());
        }

        public List<string> GetSelectColumns()
        {
            var parser = new LexalParser(QueryText);
        
            return parser.ProcessSelect().Select(x => x.ColumnName).ToList();
        }
    }
}