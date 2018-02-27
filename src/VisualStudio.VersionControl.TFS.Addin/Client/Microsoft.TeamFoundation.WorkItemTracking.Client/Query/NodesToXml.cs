// NodesToXml.cs
// 
// Authors:
//       Ventsislav Mladenov
//       Javier Suárez Ruiz
//
// Copyright (c) 2018 Ventsislav Mladenov, Javier Suárez Ruiz
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
using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Client.Query.Where;

namespace Microsoft.TeamFoundation.WorkItemTracking.Client.Query
{
    sealed class NodesToXml
    {
        readonly NodeList nodes;

        public NodesToXml(NodeList nodes)
        {
            this.nodes = nodes;
        }

        void WriteExpression(ConditionNode node, XmlWriter writer)
        {
            writer.WriteStartElement("Expression");
            var fieldNode = ((FieldNode)node.Left);
            writer.WriteAttributeString("Column", fieldNode.Field);
            writer.WriteAttributeString("FieldType", fieldNode.FieldType.ToString());
            writer.WriteAttributeString("Operator", node.ToOperator());
            var constantNode = (ConstantNode)node.Right; //All Nodes to Right should be Constants.
            var strValue = Convert.ToString(constantNode.Value, CultureInfo.InvariantCulture);
            writer.WriteElementString(constantNode.DataType.ToString(), strValue);
            writer.WriteEndElement();
        }

        void WriterStartGroup(OperatorNode operatorNode, XmlWriter writer)
        {
            writer.WriteStartElement("Group");
            writer.WriteAttributeString("GroupOperator", operatorNode.Operator.ToString());
        }

        internal string WriteXml()
        {
            StringBuilder builder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;

            using (XmlWriter writer = XmlWriter.Create(builder, settings))
            {
                if (nodes.Count == 1 && nodes[0].NodeType == NodeType.Condition)
                {
                    WriteExpression((ConditionNode)nodes[0], writer);
                }
                else
                {
                    if (nodes[0].NodeType != NodeType.Operator)
                        throw new Exception("Invalid Node Order");
                    
                    var operatorNode = (OperatorNode)nodes[0];
                    WriterStartGroup(operatorNode, writer);
                  
                    for (int i = 1; i < nodes.Count; i++)
                    {
                        var node = nodes[i];
                        if (node.NodeType == NodeType.OpenBracket)
                        {
                            i++;
                            var op = (OperatorNode)nodes[i];
                            WriterStartGroup(op, writer);
                            continue;
                        }
                        if (node.NodeType == NodeType.CloseBracket)
                            writer.WriteEndElement();
                        if (node.NodeType == NodeType.Condition)
                            WriteExpression((ConditionNode)node, writer);
                    }

                    writer.WriteEndElement();
                }
            }

            return builder.ToString();
        }
    }
}