using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestListDelta
    {
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
        public void TestListDeltaMerge()
        {
            // subsequent deletes
            {
                var d = new ListDelta<int>();
                var d2 = new ListDelta<int>();

                d.Delete(2, new[] { 1, 2, 3 });
                d2.Delete(2, new[] { 4, 5, 6 });

                d.Add(d2);

                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 2,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 1, 2, 3, 4, 5, 6 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
            }

            // subsequent updates
            {
                var d = new ListDelta<int>();
                d.Update(2, 1, 2);
                var d2 = new ListDelta<int>();
                d2.Update(3, 1, 2);
                d.Add(d2);
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 2,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 2, 2 },
                            DeleteItems = new[] { 1, 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

        }

        [TestMethod]
        public void TestListDeltaSimple()
        {
            // single delete
            {
                var d = new ListDelta<int>();
                d.Delete(3, new[] { 1 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 3,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            DeleteItems = new[] { 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            // subsequent deletes
            {
                var d = new ListDelta<int>();
                d.Delete(3, new[] { 1 });
                d.Delete(3, new[] { 2 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 3,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            DeleteItems = new[] { 1, 2 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            // subsequent deletes
            {
                var d = new ListDelta<int>();
                d.Delete(1, new[] { 1 });
                d.Delete(0, new[] { 0 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 0,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            DeleteItems = new[] { 0, 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            // single insert
            {
                var d = new ListDelta<int>();
                d.Insert(3, new[] { 1 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 3,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            // subsequent inserts
            {
                var d = new ListDelta<int>();
                d.Insert(3, new[] { 1 });
                d.Insert(3, new[] { 2 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 3,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 2, 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            // insert and delete complementing (update)
            {
                var d = new ListDelta<int>();
                d.Delete(3, new[] { 1 });
                d.Insert(3, new[] { 2 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 3,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 2 },
                            DeleteItems = new[] { 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            // multiple updates
            {
                var d = new ListDelta<int>();
                d.Update(3, 1, 2);
                d.Update(3, 2, 3);
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 3,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 3 },
                            DeleteItems = new[] { 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            // insert and delete offsetting
            {
                var d = new ListDelta<int>();
                d.Insert(3, new[] { 1 });
                d.Delete(3, new[] { 1 });
                Assert.IsTrue(!d.IndexDeltas.Any());
            }

            // interleaved offsetting delete/inserts
            {
                var d = new ListDelta<int>();
                d.Delete(0, new[] { 1, 2, 3, 4, 5, 7 });
                d.Insert(0, new[] { 1, 2, 4, 5, 6, 7 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 2,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            DeleteItems = new[] { 3 },
                        },
                    },
                    new IndexNode<int>
                    {
                        Index = 4,
                        DenseIndex = 1,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 6 },
                        },
                    }
                }, new IndexNodeComparer<int>()));
            }
        }

        [TestMethod]
        public void TestListDeltaCanonical()
        {
            {
                var d = new ListDelta<int>();
                d.Insert(0, new[] { 100 });
                d.Insert(1, new[] { 101 });
                d.Delete(2, new[] { 0, 1 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 0,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 100, 101 },
                            DeleteItems = new[] { 0, 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            {
                var d = new ListDelta<int>();
                d.Delete(0, new[] { 0, 1 });
                d.Insert(0, new[] { 100 });
                d.Insert(1, new[] { 101 });
                Assert.IsTrue(d.IndexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 0,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 100, 101 },
                            DeleteItems = new[] { 0, 1 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            {
                var d = new ListDelta<int>();
                d.Delete(0, new[] { 100 });
                d.Delete(2, new[] { 102 });
                d.Delete(1, new[] { 101 });

                var indexDeltas = d.IndexDeltas.ToArray();
                Assert.IsTrue(indexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 0,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new int[] {  },
                            DeleteItems = new[] { 100 },
                        },
                    },
                    new IndexNode<int>
                    {
                        Index = 1,
                        DenseIndex = 1,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new int[] {  },
                            DeleteItems = new[] { 101, 102 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }

            // case where the root node gets canonicalized away
            {
                var d = new ListDelta<int>();
                d.Update(4, 4, 104);
                d.Update(6, 6, 106);
                d.Update(2, 2, 102);
                d.Update(8, 8, 108);
                d.Update(3, 3, 103); // <== root changed from 104 to 106

                var indexDeltas = d.IndexDeltas.ToArray();
                Assert.IsTrue(indexDeltas.SequenceEqual(new[]
                {
                    new IndexNode<int>
                    {
                        Index = 2,
                        DenseIndex = 0,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 102, 103, 104 },
                            DeleteItems = new[] { 2, 3, 4 },
                        },
                    },
                    new IndexNode<int>
                    {
                        Index = 6,
                        DenseIndex = 1,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 106 },
                            DeleteItems = new[] { 6 },
                        },
                    },
                    new IndexNode<int>
                    {
                        Index = 8,
                        DenseIndex = 2,
                        Data = new ListIndexDelta<int>
                        {
                            InsertItems = new[] { 108 },
                            DeleteItems = new[] { 8 },
                        },
                    },
                }, new IndexNodeComparer<int>()));
            }
        }

        [TestMethod]
        public void TestVirtualListDeltaUpdates()
        {
            const int size = 10;
            var r = new Random(1);

            for (var o = 0; o < 10000; o++)
            {
                var v = new ListDelta<int>();
                var l = new List<int>(Enumerable.Range(0, size).Select(i => i * 1000000));
                for (var i = 0; i < 30; i++)
                {
                    var index = r.Next(size);
                    var newValue = l[index] + 1;
                    v.Update(index, l[index], newValue);
                    l[index] = newValue;
                }
            }
        }

        [TestMethod]
        public void TestListDeltaEmpty()
        {
            {
                var d = new ListDelta<int>();

                d.Insert(0, new[] { 1 });
                d.Delete(0, new[] { 1 });

                Assert.IsTrue(d.IndexDeltas.SequenceEqual(
                    new IndexNode<int>[0],
                    new IndexNodeComparer<int>()));
            }

            {
                var d = new ListDelta<int>();

                d.Delete(0, new[] { 1 });
                d.Insert(0, new[] { 1 });

                Assert.IsTrue(d.IndexDeltas.SequenceEqual(
                    new IndexNode<int>[0],
                    new IndexNodeComparer<int>()));
            }
        }
    }
}
