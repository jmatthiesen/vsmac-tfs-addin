using System;
using MonoDevelop.VersionControl.TFS.Models;
using NUnit.Framework;

namespace MonoDevelop.VersionControl.TFS.Tests.Tests
{
    [TestFixture]
    public class NodeListTests
    {
        [Test]
        public void RemoveUnUsedBracketsTest()
        {
            string val = "where (([a] = 2))";
            var parser = new LexalParser(val);
            var nodes = parser.ProcessWherePart();
            nodes.Optimize();
            Console.WriteLine(nodes);

            Assert.AreEqual("[a] = 2", nodes.ToString());
        }

        [Test]
        public void GetSubListTest()
        {
            string val = "where ([a] = 2) and ([b] = @p)";
            var parser = new LexalParser(val);
            var nodes = parser.ProcessWherePart();
            var list = nodes.GetSubList(0);
            Console.WriteLine(list);

            Assert.AreEqual("[a] = 2", list.ToString());
        }

        [Test]
        public void ExtractOperatorForwardTest()
        {
            string val = "where [a] = 2 and [b] = 3 and [c] = 4 or [d] = 5";
            var parser = new LexalParser(val);
            var nodes = parser.ProcessWherePart();
            nodes.Optimize();
            nodes.ExtractOperatorForward();
            Console.WriteLine(nodes);

            Assert.AreEqual("Or ( And [a] = 2 [b] = 3 [c] = 4 ) [d] = 5", nodes.ToString());
        }
    }
}
