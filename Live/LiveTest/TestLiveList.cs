using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using Vertigo.Live;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLiveList
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
        public void TestListSimple()
        {
            var x = new List<int> { 1 }.ToLiveList();
            var i = x.ToIndependent();

            i.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1 }));
                    Assert.IsTrue(s.Delta == null);
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(2),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2 }));
                    Assert.IsTrue(s.Delta.Inserts.SequenceEqual(new[] { 2 }));
                });

            i.States().Skip(1).Take(1).Check(
                () =>
                {
                    x.PublishInner.Insert(0, 0);
                    x.PublishInner.RemoveAt(2);
                },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 1 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 0 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 2,
                            DenseIndex = 1,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 2 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
        }

        [TestMethod]
        public void TestListWhere()
        {
            var x = new List<int> { 1, 2, 3, 4, 5 }.ToLiveList();
            var w = x.Where(i => i <= 3).ToIndependent();

            w.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 3 }));
                    Assert.IsTrue(s.Delta == null);
                });

            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(0),
                state =>
                {
                    var s = state.Single();

                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 3, 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 3,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 0 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(3),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
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
                    }, new IndexNodeComparer<int>()));
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(4),
                results => Assert.AreEqual(0, results.Count()));
        }

        [TestMethod]
        public void TestLiveListWhere()
        {
            var x = new[] { 1, 2, 3, 4, 5 }
                .Select(i => i.ToLive())
                .ToList()
                .ToLiveList();
            var w = x.Where(i => i.LessThanOrEqual(3.ToLive())).ToIndependent();

            w.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.Select(i => i.Value).SequenceEqual(new[] { 1, 2, 3 }));
                    Assert.IsTrue(s.Delta == null);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(0.ToLive()),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.Select(i => i.Value).SequenceEqual(new[] { 1, 2, 3, 0 }));
                    Assert.IsTrue(s.Delta.Inserts.Select(i => i.Value).SequenceEqual(new[] { 0 }));
                    Assert.IsTrue(s.Delta.Deletes == null);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(x.ToArray().Single(i => i.PublishValue == 3)),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.Select(i => i.Value).SequenceEqual(new[] { 1, 2, 0 }));
                    Assert.IsTrue(s.Delta.Inserts == null);
                    Assert.IsTrue(s.Delta.Deletes.Select(i => i.Value).SequenceEqual(new[] { 3 }));
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(x.ToArray().Single(i => i.PublishValue == 4)),
                results => Assert.AreEqual(0, results.Count()));
        }

        [TestMethod]
        public void TestLiveListUnwrap()
        {
            var x = new[] { 1, 2, 3, 4, 5 }
                .Select(i => i.ToLive())
                .ToList()
                .ToLiveList();
            var w = x.Unwrap<int>().ToIndependent();

            w.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
                    Assert.IsTrue(s.Delta == null);
                });

            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(6.ToLive()),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 5,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 6 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.RemoveAt(2),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 4, 5, 6 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
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
                    }, new IndexNodeComparer<int>()));
                });

            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Insert(0, 0.ToLive()),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 1, 2, 4, 5, 6 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 0 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.RemoveAt(1),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 2, 4, 5, 6 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 1,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 1 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

        }

        [TestMethod]
        public void TestLiveListSelectMany()
        {
            var x = new[] { 0 }
                .ToCollection()
                .ToLiveCollection();
            var y = new[] { 100 }
                .ToCollection()
                .ToLiveCollection();
            var w = x
                .SelectMany(x1 => y.SelectStatic(y1 => Tuple.Create(x1, y1)))
                .ToIndependent();

            w.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[]
                        {
                            Tuple.Create(0, 100), 
                        }));
                    Assert.IsTrue(s.Delta == null);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(1),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[]
                        {
                            Tuple.Create(0, 100), 
                            Tuple.Create(1, 100), 
                        }));
                    Assert.IsTrue(s.Delta.Inserts.UnorderedEqual(new[]
                        {
                            Tuple.Create(1, 100), 
                        }));
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(1),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[]
                        {
                            Tuple.Create(0, 100), 
                        }));
                    Assert.IsTrue(s.Delta.Deletes.UnorderedEqual(new[]
                        {
                            Tuple.Create(1, 100), 
                        }));
                });
            w.States().Skip(1).Take(2).Check(
                () =>
                {
                    x.PublishInner.Add(1);
                    y.PublishInner.Add(101);
                },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[]
                        {
                            Tuple.Create(0, 100),
                            Tuple.Create(0, 101),
                            Tuple.Create(1, 100),
                            Tuple.Create(1, 101),
                        }));
                    Assert.IsTrue(s.Delta.Inserts.UnorderedEqual(new[]
                        {
                            Tuple.Create(0, 101),
                            Tuple.Create(1, 100),
                            Tuple.Create(1, 101),
                        }));
                });
        }

        [TestMethod]
        public void TestLiveListCrossJoin()
        {
            var x = new[] { 0 }
                .ToCollection()
                .ToLiveCollection();
            var w = x
                .SelectMany(s1 => x.SelectStatic(s2 => Tuple.Create(s1, s2)))
                .ToIndependent();

            w.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[]
                        {
                            Tuple.Create(0, 0), 
                        }));
                    Assert.IsTrue(s.Delta == null);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(1),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[]
                        {
                            Tuple.Create(0, 0), 
                            Tuple.Create(0, 1), 
                            Tuple.Create(1, 0), 
                            Tuple.Create(1, 1), 
                        }));
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Delta.Inserts.UnorderedEqual(new[]
                        {
                            Tuple.Create(0, 1), 
                            Tuple.Create(1, 0), 
                            Tuple.Create(1, 1), 
                        }));
                });
        }

        [TestMethod]
        public void TestListUnwrap()
        {
            var l = new List<Live<int>> { 1.ToLive() }.ToLiveList();
            var m = (ILiveList<ILiveValue<int>>)l;
            var v = l.PublishInner.First();

            m.Unwrap().ToIndependent().States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1 }));
                    Assert.IsTrue(s.Delta == null);
                });

            m.Unwrap().ToIndependent().States().Skip(1).Take(1).Check(
                () => v.PublishValue = 3,
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 3 }));
                    Assert.IsTrue(s.Delta.Inserts.SequenceEqual(new[] { 3 }));
                    Assert.IsTrue(s.Delta.Deletes.SequenceEqual(new[] { 1 }));
                });
            m.Unwrap().Sum().States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.NewValue == 3);
                });
            m.Unwrap().Sum().States().Skip(1).Take(1).Check(
                () => v.PublishValue = 5,
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.HasChange);
                    Assert.IsTrue(s.NewValue == 5);
                    Assert.IsTrue(s.OldValue == 3);
                });
        }

        [TestMethod]
        public void TestListStock()
        {
            var oldStocks = new List<OldStock>(new[]
            {
                new OldStock
                {
                    Code = "ABC",
                    Name = "ABC Limited",
                    Price = 100,
                    Turnover = 1000,
                },
                new OldStock
                {
                    Code = "DEF",
                    Name = "DEF Limited",
                    Price = 20,
                    Turnover = 100,
                },
            });

            var oOldStocks = oldStocks.ToLiveList();
            var iOldStocks = oOldStocks.PublishInner;
            var totalPrices = oOldStocks.Select(s => s.LiveProperty<decimal>("Price")).Sum();

            var stocks = new List<Stock>
            {
                new Stock
                {
                    Code = {PublishValue = "ABC"},
                    Name = {PublishValue = "ABC Limited"},
                    Price = {PublishValue = 100},
                    Turnover = {PublishValue = 1000},
                    Bid = {PublishValue = 10},
                    Ask = {PublishValue = 12},
                },
                new Stock
                {
                    Code = {PublishValue = "DEF"},
                    Name = {PublishValue = "DEF Limited"},
                    Price = {PublishValue = 20},
                    Turnover = {PublishValue = 100},
                    Bid = {PublishValue = 8},
                    Ask = {PublishValue = 11},
                },
            }.ToLiveList();

            var turnovers = stocks.Select(s => s.Turnover);
            var abc = stocks.PublishInner.Single(s => s.Code.PublishValue == "ABC");
            var def = stocks.PublishInner.Single(s => s.Code.PublishValue == "DEF");

            var turnoverSum = turnovers.Sum();
            turnoverSum.Trace("turnoverSum");

            Assert.AreEqual(StateStatus.Connecting, turnoverSum.Snapshot.Status);
            Assert.AreEqual(turnoverSum.Value, 1100);

            using (Publish.Transaction(true))
            {
                abc.Turnover.PublishValue = 999;
                Assert.AreEqual(1100, turnoverSum.Value);
            }

            using (Publish.Transaction(true))
            {
                Assert.AreEqual(1099, turnoverSum.Value);
                abc.Turnover.PublishValue = 997;
                abc.Turnover.PublishValue = 998;
                Assert.AreEqual(1099, turnoverSum.Value);
            }

            Assert.AreEqual(1098, turnoverSum.Value);

            var overlap = stocks
                .CrossJoin(stocks)
                .Where(ab => ab.Item1.Bid.GreaterThan(ab.Item2.Ask));
            var overlap0 = overlap.States().Subscribe(c =>
            {
                switch (c.Status)
                {
                    case StateStatus.Connecting:
                        Debug.Print("START -- {0}",
                            string.Join(";", c.Inner.Select(i => string.Format("{0}-{1}", i.Item1.Code.Value, i.Item2.Code.Value))));
                        break;
                    case StateStatus.Connected:
                        if (!c.Delta.HasChange())
                            break;
                        Debug.Print("DELTA -- Add: {0} Remove: {1}",
                            c.Delta.Inserts == null ? null : string.Join(";", c.Delta.Inserts.Select(i => string.Format("{0}-{1}", i.Item1.Code.Value, i.Item2.Code.Value))),
                            c.Delta.Deletes == null ? null : string.Join(";", c.Delta.Deletes.Select(i => string.Format("{0}-{1}", i.Item1.Code.Value, i.Item2.Code.Value))));
                        break;
                }
            });

            abc.Bid.PublishValue = 11.5M;
            def.Ask.PublishValue = 12;
        }

        [TestMethod]
        public void TestListConflictingTimelines()
        {
            DispatcherConsumer.Dispatcher = new DispatcherConsumer(Dispatcher.CurrentDispatcher, TimeSpan.FromMilliseconds(10));

            var list = Enumerable
                .Range(0, 10)
                .Select(v => -1)
                .ToList()
                .ToLiveList();

            // subscribe synchronously
            List<int> ints = null;
            List<object> o = null;
            list.States()
                .Subscribe(c =>
                {
                    Debug.Print("ints: {0}", c);
                    switch (c.Status)
                    {
                        case StateStatus.Connecting:
                            ints = new List<int>(c.Inner);
                            break;
                        case StateStatus.Connected:
                            if (!c.Delta.HasChange())
                                break;
                            c.Delta.ToMutable<int, IListDelta<int>, IList<int>>().ApplyTo(ints);
                            break;
                    }
                });
            list.Cast<int, object>()
                .States()
                .Subscribe(c =>
                {
                    Debug.Print("o: {0}", c);
                    switch (c.Status)
                    {
                        case StateStatus.Connecting:
                            o = new List<object>(c.Inner);
                            break;
                        case StateStatus.Connected:
                            Debug.Print("{0}", c.Delta.IndexDeltas);
                            c.Delta.ToMutable<object, IListDelta<object>, IList<object>>().ApplyTo(o);
                            break;
                    }
                });

            for (var i = 0; i < 1000; i++)
            {
                using (Publish.Transaction(true))
                {
                    for (var j = 0; j < list.PublishInner.Count; j++)
                        list.PublishInner[j] = i;
                }
            }
        }

        [TestMethod]
        public void TestListTakeSimple()
        {
            var l = new List<int>().ToLiveList();
            l.PublishInner.Connect(Enumerable.Empty<int>());
            var t = l.Take(3.ToLiveConst()).ToIndependent();

            t.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new int[] { }));
                    Assert.IsTrue(s.Delta == null);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Add(0),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 0 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
        }

        [TestMethod]
        public void TestListTake()
        {
            var l = new List<int> { 1, 2, 3, 4, 5 }.ToLiveList();
            var t = l.Take(3.ToLiveConst()).ToIndependent();

            t.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 3 }));
                    Assert.IsTrue(s.Delta == null);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Insert(0, 0),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 1, 2 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 0 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 3,
                            DenseIndex = 1,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.RemoveAt(0),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 3 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 0 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 2,
                            DenseIndex = 1,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner[1] = 99,
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 99, 3 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 1,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 99 },
                                DeleteItems = new[] { 2 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner[2] = 88,
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 99, 88 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 2,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 88 },
                                DeleteItems = new[] { 3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
            t.Check1(
                () =>
                {
                    l.PublishInner.Insert(0, -1);
                    l.PublishInner.Insert(3, 77);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { -1, 1, 99 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { -1 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 3,
                            DenseIndex = 1,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 88 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner[3] = -1,
                results => Assert.AreEqual(0, results.Count()));
        }

        [TestMethod]
        public void TestListTakeComplex()
        {
            var l = new List<int> { -1, -2, -3, -4, -5 }.ToLiveList();
            var t = l.Take(3.ToLiveConst()).ToIndependent();

            t.Check1(
                () => Enumerable.Range(0, 5).ForEach(i => l.PublishInner.Insert(i, 100 + i)),
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 100, 101, 102 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 100, 101, 102 },
                                DeleteItems = new[] { -1, -2, -3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            l = new List<int> { -1, -2, -3, -4, -5 }.ToLiveList();
            t = l.Take(5.ToLiveConst()).ToIndependent();

            t.Check1(
                () =>
                {
                    l.PublishInner.Insert(1, 100);
                    l.PublishInner.Insert(3, 200);
                    l.PublishInner.Insert(5, 300);
                    l.PublishInner.Insert(7, 400);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { -1, 100, -2, 200, -3 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 1,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 100 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 3,
                            DenseIndex = 1,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 200 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 5,
                            DenseIndex = 2,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { -4, -5 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            l = new List<int> { -1, -2, -3, -4, -5 }.ToLiveList();
            t = l.Take(5.ToLiveConst()).ToIndependent();

            t.Check1(
                () =>
                {
                    l.PublishInner.Insert(1, 100);
                    l.PublishInner.Insert(3, 200);
                    l.PublishInner.RemoveAt(4);
                    l.PublishInner.Insert(5, 300);
                    l.PublishInner.Insert(7, 400);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { -1, 100, -2, 200, -4 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 1,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 100 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 3,
                            DenseIndex = 1,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 200 },
                                DeleteItems = new[] { -3 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 5,
                            DenseIndex = 2,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { -5 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
        }

        [TestMethod]
        public void TestListTakeComplex2()
        {
            var l = new List<int>().ToLiveList();
            var t = l.Take(3.ToLiveConst()).ToIndependent();

            t.Check1(
                () =>
                {
                    l.PublishInner.Insert(0, 1);
                    l.PublishInner.Insert(1, 2);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 1, 2 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            t.Check1(
                () =>
                {
                    l.PublishInner.RemoveAt(0);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 2 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 1 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            t.Check1(
                () =>
                {
                    l.PublishInner.Insert(1, 3);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 2, 3 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 1,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

        }

        [TestMethod]
        public void TestListSkip()
        {
            var l = new List<int> { 1, 2, 3, 4, 5 }.ToLiveList();
            var t = l.Skip(3.ToLiveConst()).ToIndependent();

            t.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 4, 5 }));
                    Assert.IsTrue(s.Delta == null);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Insert(0, 0),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 3, 4, 5 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.RemoveAt(0),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 4, 5 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner[1] = 99,
                state => Assert.AreEqual(0, state.Count()));

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner[3] = 88,
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 88, 5 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 88 },
                                DeleteItems = new[] { 4 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            t.Check1(
                () =>
                {
                    l.PublishInner.Insert(0, -1);
                    l.PublishInner.Insert(3, 77);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 77, 3, 88, 5 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 77, 3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner[2] = -1,
                state => Assert.AreEqual(0, state.Count()));
        }

        [TestMethod]
        public void TestListSkipComplex()
        {
            var l = new List<int> { -1, -2, -3, -4, -5 }.ToLiveList();
            var t = l.Skip(3.ToLiveConst()).ToIndependent();

            t.Check1(
                () => Enumerable.Range(0, 5).ForEach(i => l.PublishInner.Insert(i, 100 + i)),
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 103, 104, -1, -2, -3, -4, -5 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 103, 104, -1, -2, -3 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            l = new List<int> { -1, -2, -3, -4, -5 }.ToLiveList();
            t = l.Skip(3.ToLiveConst()).ToIndependent();

            t.Check1(
                () =>
                {
                    l.PublishInner.Insert(1, 100);
                    l.PublishInner.Insert(3, 200);
                    l.PublishInner.Insert(5, 300);
                    l.PublishInner.Insert(7, 400);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 200, -3, 300, -4, 400, -5 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 200, -3, 300 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 4,
                            DenseIndex = 1,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 400 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            l = new List<int> { -1, -2, -3, -4, -5 }.ToLiveList();
            t = l.Skip(3.ToLiveConst()).ToIndependent();

            t.Check1(
                () =>
                {
                    l.PublishInner.Insert(1, 100);
                    l.PublishInner.Insert(3, 200);
                    l.PublishInner.RemoveAt(4);
                    l.PublishInner.Insert(5, 300);
                    l.PublishInner.Insert(7, 400);
                },
                state =>
                {
                    var s = state.Skip(1).First();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 200, -4, 300, -5, 400 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 200 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 2,
                            DenseIndex = 1,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 300 },
                            },
                        },
                        new IndexNode<int>
                        {
                            Index = 4,
                            DenseIndex = 2,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 400 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
        }

        [TestMethod]
        public void TestListOrderBy()
        {
            var l = new HashSet<int>().ToLiveSet();
            var t = l.OrderByStatic(i => i).ToIndependent();

            t.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new int[] { }));
                    Assert.IsTrue(s.Delta == null);
                    Assert.IsTrue(s.LastUpdated == 0);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Add(20, 1000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 20 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 20 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                    Assert.IsTrue(s.LastUpdated == 1000);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Add(10, 2000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 10, 20 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 10 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                    Assert.IsTrue(s.LastUpdated == 2000);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Add(30, 3000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 10, 20, 30 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 2,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 30 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                    Assert.IsTrue(s.LastUpdated == 3000);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Add(0, 4000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 10, 20, 30 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 0 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                    Assert.IsTrue(s.LastUpdated == 4000);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Add(15, 5000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 10, 15, 20, 30 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 2,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 15 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                    Assert.IsTrue(s.LastUpdated == 5000);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Remove(15, 6000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 10, 20, 30 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 2,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 15 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                    Assert.IsTrue(s.LastUpdated == 6000);
                });

            t.States().Skip(1).Take(1).Check(
                () => l.PublishInner.Connect(new[] { 20, 10 }, 7000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Reconnecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 10, 20 }));
                    Assert.IsTrue(s.Delta == null);
                    Assert.IsTrue(s.LastUpdated == 7000);
                });
        }

        [TestMethod]
        public void TestListZip()
        {
            var a = new[] { 1, 2, 3, 4, 5 }.ToList().ToLiveList();
            var b = new[] { -1, -2, -3, -4, -5 }.ToList().ToLiveList();
            var z = a.Zip(b).ToIndependent();

            z.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(
                        new[]
                            {
                                Tuple.Create(1, -1),
                                Tuple.Create(2, -2),
                                Tuple.Create(3, -3),
                                Tuple.Create(4, -4),
                                Tuple.Create(5, -5),
                            }));
                    Assert.IsTrue(s.Delta == null);
                    Assert.IsTrue(s.LastUpdated == 0);
                });

        }

        [TestMethod]
        public void TestListReverse()
        {
            var x = new List<int> { 1, 2, 3 }.ToLiveList();
            var i = x.Reverse().ToIndependent();

            i.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 3, 2, 1 }));
                    Assert.IsTrue(s.Delta == null);
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Insert(0, 0),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 3, 2, 1, 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 3,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    InsertItems = new[] { 0 },
                                },
                            }
                        }, new IndexNodeComparer<int>()));
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Insert(4, 4),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 4, 3, 2, 1, 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 0,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    InsertItems = new[] { 4 },
                                },
                            }
                        }, new IndexNodeComparer<int>()));
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Insert(2, -1),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 4, 3, 2, -1, 1, 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 3,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    InsertItems = new[] { -1 },
                                },
                            }
                        }, new IndexNodeComparer<int>()));
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.RemoveAt(2),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 4, 3, 2, 1, 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 3,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    DeleteItems = new[] { -1 },
                                },
                            }
                        }, new IndexNodeComparer<int>()));
                });

            i.States().Skip(1).Take(1).Check(
                () =>
                {
                    x.PublishInner.Insert(2, -1);
                    x.PublishInner.RemoveAt(4);
                },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 4, 2, -1, 1, 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 1,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    DeleteItems = new[] { 3 },
                                },
                            },
                            new IndexNode<int>
                            {
                                Index = 2,
                                DenseIndex = 1,
                                Data = new ListIndexDelta<int>
                                {
                                    InsertItems = new[] { -1 },
                                },
                            },
                        }, new IndexNodeComparer<int>()));
                });

            i.States().Skip(1).Take(1).Check(
                () =>
                {
                    x.PublishInner.RemoveAt(2);
                    x.PublishInner.Insert(3, 3);
                },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 4, 3, 2, 1, 0 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 1,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    InsertItems = new[] { 3 },
                                },
                            },
                            new IndexNode<int>
                            {
                                Index = 3,
                                DenseIndex = 1,
                                Data = new ListIndexDelta<int>
                                {
                                    DeleteItems = new[] { -1 },
                                },
                            },
                        }, new IndexNodeComparer<int>()));
                });
        }

        [TestMethod]
        public void TestListConcat()
        {
            var x = new List<int> { 1, 2 }.ToLiveList();
            var y = new List<int> { 3, 4 }.ToLiveList();
            var i = x.Concat(y).ToIndependent();

            i.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 3, 4 }));
                    Assert.IsTrue(s.Delta == null);
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Insert(0, 0),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 1, 2, 3, 4 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 0,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    InsertItems = new[] { 0 },
                                },
                            }
                        }, new IndexNodeComparer<int>()));
                });

            i.States().Skip(1).Take(1).Check(
                () => y.PublishInner.Insert(1, -1),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 1, 2, 3, -1, 4 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 4,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    InsertItems = new[] { -1 },
                                },
                            }
                        }, new IndexNodeComparer<int>()));
                });

            i.States().Skip(1).Take(1).Check(
                () => y.PublishInner.RemoveAt(1),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0, 1, 2, 3, 4 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 4,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    DeleteItems = new[] { -1 },
                                },
                            }
                        }, new IndexNodeComparer<int>()));
                });

            i.States().Skip(1).Take(1).Check(
                () =>
                    {
                        x.PublishInner.RemoveAt(0);
                        y.PublishInner.RemoveAt(0);
                    },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1, 2, 4 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 0,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    DeleteItems = new[] { 0 },
                                },
                            },
                            new IndexNode<int>
                            {
                                Index = 2,
                                DenseIndex = 1,
                                Data = new ListIndexDelta<int>
                                {
                                    DeleteItems = new[] { 3 },
                                },
                            }
                        },
                        new IndexNodeComparer<int>()));
                });
        }

        [TestMethod]
        public void TestListDefaultIfEmpty()
        {
            var x = new List<int> { 1 }.ToLiveList();
            var i = x.DefaultIfEmpty().ToIndependent();

            i.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 1 }));
                    Assert.IsTrue(s.Delta == null);
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.RemoveAt(0),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Reconnecting);
                    Assert.IsFalse(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 0 }));
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(2),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Reconnecting);
                    Assert.IsFalse(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 2 }));
                });

            i.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(3),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.SequenceEqual(new[] { 2, 3 }));
                    Assert.IsTrue(s.Delta.IndexDeltas.SequenceEqual(new[]
                        {
                            new IndexNode<int>
                            {
                                Index = 1,
                                DenseIndex = 0,
                                Data = new ListIndexDelta<int>
                                {
                                    InsertItems = new[] { 3 },
                                },
                            }
                        }, new IndexNodeComparer<int>()));
                });
        }
    }
}
