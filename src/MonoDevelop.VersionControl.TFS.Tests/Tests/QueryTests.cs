using System.Xml.Linq;
using MonoDevelop.VersionControl.TFS.Models;
using NUnit.Framework;

namespace MonoDevelop.VersionControl.TFS.Tests.Tests
{
    [TestFixture]
    public class QueryTests
    {
        [Test]
        public void OptimizeTest()
        {
            XElement el = XElement.Parse(@"<f>
            SELECT [System.Id], [System.WorkItemType], [Microsoft.VSTS.Common.Rank], [System.Title], [System.State], [System.AssignedTo], [Microsoft.VSTS.Common.RoughOrderOfMagnitude], [Microsoft.VSTS.Common.ExitCriteria], [System.Description] 
            FROM WorkItems 
            WHERE [System.TeamProject] = @project 
              AND [System.WorkItemType] = 'Scenario' 
              AND [System.State] = 'Active' 
            ORDER BY [Microsoft.VSTS.Common.Rank], [System.State], [System.Id]
            </f>");
            
            var parser = new LexalParser(el.Value);
            var nodes = parser.ProcessWherePart();
            nodes.Optimize();

            Assert.IsTrue(nodes[0].NodeType == NodeType.Condition);
            Assert.IsTrue(((ConditionNode)nodes[0]).Right.NodeType == NodeType.Parameter);

            Assert.IsTrue(nodes[1].NodeType == NodeType.Operator);

            Assert.IsTrue(nodes[2].NodeType == NodeType.Condition);
            Assert.IsTrue(((ConditionNode)nodes[2]).Right.NodeType == NodeType.Constant);

            Assert.IsTrue(nodes[3].NodeType == NodeType.Operator);

            Assert.IsTrue(nodes[4].NodeType == NodeType.Condition);
            Assert.IsTrue(((ConditionNode)nodes[4]).Right.NodeType == NodeType.Constant);
        }
    }
}
