using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestVirtualList
    {
        public TestVirtualList()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion


        [TestMethod]
        public void TestVirtualListSimple()
        {
            var v = new VirtualList<int>(false);

            v.Insert(0, 0);
            v.Check();
            v.Insert(1, 1);
            v.Check();
            v.RemoveAt(0);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 1 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 1 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0 }));
        }

        [TestMethod]
        public void TestVirtualListSimple1()
        {
            var v = new VirtualList<int>(false);

            v.Insert(0, 0);
            v.Check();
            v.Insert(5, 1);
            v.Check();
            v.Insert(10, 2);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 2 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 0, 1, 2 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 5, 10 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0, 1, 2 }));

            v.Insert(2, 3);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 0, 0, 3, 0, 0, 0, 1, 0, 0, 0, 0, 2 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 0, 3, 1, 2 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 2, 6, 11 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0, 1, 2, 3 }));
        }

        [TestMethod]
        public void TestVirtualListSimple2()
        {
            var v = new VirtualList<int>(false);

            v.Insert(2, 100);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 0, 0, 100 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 100 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 2 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0 }));

            v.Insert(2, 200);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 0, 0, 200, 100 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 200, 100 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2, 3 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 2, 3 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0, 1 }));

            v.RemoveAt(2);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 0, 0, 100 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 100 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 2 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0 }));

            v.RemoveAt(0);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 0, 100 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 100 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 1 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0 }));
        }

        [TestMethod]
        public void TestVirtualListSimple3()
        {
            var v = new VirtualList<int>(false, new[] { 1, 2, 3 });

            v.Add(0);

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 1, 2, 3, 0 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 1, 2, 3, 0 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2, 3 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2, 3 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0, 1, 2, 3 }));

            v.Remove(2);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 1, 3, 0 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 1, 3, 0 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0, 1, 2 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0, 1, 2 }));
        }

        [TestMethod]
        public void TestVirtualListSimple4()
        {
            var v = new VirtualList<int>(false);

            v.Add(0);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0 }));

            v.RemoveAt(0);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new int[0]));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new int[0]));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new int[0]));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new int[0]));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new int[0]));
        }

        [TestMethod]
        public void TestVirtualListSimple5()
        {
            var v = new VirtualList<int>(false);

            v.Insert(0, 1);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 1 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 1 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0 }));

            v.Insert(1, 2);
            v.Check();
            v.RemoveAt(0);
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 2 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 2 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0 }));

            v[0] = 3;
            v.Check();

            Assert.IsTrue(v.ToArray().SequenceEqual(new[] { 3 }));
            Assert.IsTrue(v.Dense.ToArray().SequenceEqual(new[] { 3 }));
            Assert.IsTrue(v.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.Index).SequenceEqual(new[] { 0 }));
            Assert.IsTrue(v.Dense.Nodes.Select(n => n.DenseIndex).SequenceEqual(new[] { 0 }));
        }

        [TestMethod]
        public void TestVirtualListComplex()
        {
            var r = new Random(0);
            var v = new VirtualList<int>(false, Enumerable.Range(1, 10));
            v.Check();

            for (int i = 11; i < 5000; i++)
            {
                switch (r.Next(2))
                {
                    case 0:
                        var val = r.Next(v.Count + 1);
                        //Debug.Print("{0}: insert {1} @ {2}", v.Count, val, i);
                        v.Insert(val, i);
                        break;
                    case 1:
                        if (v.Count > 0)
                        {
                            var idx = r.Next(v.Count);
                            //Debug.Print("{0}: delete @ {1}", v.Count, idx);
                            v.RemoveAt(idx);
                        }
                        break;
                }

                v.Check();
            }
        }

        [TestMethod]
        public void TestVirtualListComplex1()
        {
            var r = new Random();
            var v = new VirtualList<int>(false);

            v.Insert(0, 106);
            v.Check();
            v.Insert(0, 107);
            v.Check();
            v.Insert(0, 108);
            v.Check();
            v.Insert(0, 109);
            v.Check();
            v.Insert(2, 110);
            v.Check();
            v.Insert(2, 111);
            v.Check();

            v.RemoveAt(1);
            v.Check();
            v.RemoveAt(0);
            v.Check();
            v.RemoveAt(3);
            v.Check();
        }
    }
}
