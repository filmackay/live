using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLiveCollection
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
        public void TestCollectionSimple()
        {
            var c = new int[] { }.ToCollection().ToLiveCollection();
            var d = c.ToIndependent();

            d.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.Inner.Count());
                    Assert.AreEqual(null, state.Delta);
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(1),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1 }));
                    Assert.IsTrue(state.Delta.Inserts.UnorderedEqual(new[] { 1 }));
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Remove(1),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new int[0]));
                    Assert.IsTrue(state.Delta.Deletes.UnorderedEqual(new[] { 1 }));
                });
        }

        [TestMethod]
        public void TestLiveCollectionPublisher()
        {
            var a = new int[] { }
                .ToCollection()
                .ToLiveCollection();

            a.Trace("a");

            using (Publish.Transaction(true))
            {
                a.PublishInner.Connect(new[] {1, 2, 3}, 1000);
            }

            using (Publish.Transaction(true))
            {
                a.PublishInner.Connect(new[] { 4, 5, 6 }, 2000);
            }
        }

        [TestMethod]
        public void TestCollectionUnwrap()
        {
            var l = new Live<int>[0].ToCollection().ToLiveCollection();

            l.Unwrap().ToIndependent().States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Debug.Assert(s.Status == StateStatus.Connecting);
                    Debug.Assert(s.Inner.SequenceEqual(new int[] { }));
                    Debug.Assert(s.Delta == null);
                });

            l.Unwrap().ToIndependent().States().Skip(1).Take(1).Check(
                () => l.PublishInner.Add(1.ToLive()),
                state =>
                {
                    var s = state.Single();
                    Debug.Assert(s.Status == StateStatus.Connected);
                    Debug.Assert(s.Delta.HasChange());
                    Debug.Assert(s.Inner.UnorderedEqual(new[] { 1 }));
                    Debug.Assert(s.Delta.Inserts.UnorderedEqual(new[] { 1 }));
                });

            l.Unwrap().ToIndependent().States().Skip(1).Take(1).Check(
                () => l.PublishInner.Add(2.ToLive()),
                state =>
                {
                    var s = state.Single();
                    Debug.Assert(s.Status == StateStatus.Connected);
                    Debug.Assert(s.Delta.HasChange());
                    Debug.Assert(s.Inner.UnorderedEqual(new[] { 1, 2 }));
                    Debug.Assert(s.Delta.Inserts.UnorderedEqual(new[] { 2 }));
                });

            l.Unwrap().ToIndependent().States().Skip(1).Take(1).Check(
                () => l.PublishInner.Single(i => i.Value == 2).PublishValue = 3,
                state =>
                {
                    var s = state.Single();
                    Debug.Assert(s.Status == StateStatus.Connected);
                    Debug.Assert(s.Delta.HasChange());
                    Debug.Assert(s.Inner.UnorderedEqual(new[] { 1, 3 }));
                    Debug.Assert(s.Delta.Inserts.UnorderedEqual(new[] { 3 }));
                    Debug.Assert(s.Delta.Deletes.UnorderedEqual(new[] { 2 }));
                });

            l.Unwrap().ToIndependent().States().Skip(1).Take(1).Check(
                () => l.PublishInner.Remove(l.PublishInner.Single(i => i.Value == 3)),
                state =>
                {
                    var s = state.Single();
                    Debug.Assert(s.Status == StateStatus.Connected);
                    Debug.Assert(s.Delta.HasChange());
                    Debug.Assert(s.Inner.UnorderedEqual(new[] { 1 }));
                    Debug.Assert(s.Delta.Deletes.UnorderedEqual(new[] { 3 }));
                });

            l.Unwrap().ToIndependent().States().Skip(1).Take(1).Check(
                () => l.PublishInner.Remove(l.PublishInner.Single(i => i.Value == 1)),
                state =>
                {
                    var s = state.Single();
                    Debug.Assert(s.Status == StateStatus.Connected);
                    Debug.Assert(s.Delta.HasChange());
                    Debug.Assert(s.Inner.UnorderedEqual(new int[0]));
                    Debug.Assert(s.Delta.Deletes.UnorderedEqual(new[] { 1 }));
                });
        }

        [TestMethod]
        public void TestCollectionWhere()
        {
            var c = new int[] { }.ToCollection().ToLiveCollection();
            var d = c.Where(i => i > -1).ToIndependent();

            d.States().Take(1).Check(
                () => { },
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.Inner.Count());
                    Assert.AreEqual(null, state.Delta);

                    Assert.IsFalse(enumerator.MoveNext());
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(1),
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1 }));
                    Assert.IsTrue(state.Delta.Inserts.UnorderedEqual(new[] { 1 }));

                    Assert.IsFalse(enumerator.MoveNext());
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Remove(1),
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new int[0]));
                    Assert.IsTrue(state.Delta.Deletes.UnorderedEqual(new[] { 1 }));

                    Assert.IsFalse(enumerator.MoveNext());
                });
        }

        [TestMethod]
        public void TestCollectionRestart()
        {
            var c = new[] { 1 }.ToCollection().ToLiveCollection();
            var f = c.ToIndependent();
            f.Trace("f");

            f.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(null, state.Delta);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1 }));
                    Assert.AreEqual(0, state.LastUpdated);
                });

            f.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Connect(new[] { 1, 2, 3 }, 1000),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Reconnecting, state.Status);
                    Assert.AreEqual(null, state.Delta);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1, 2, 3 }));
                    Assert.AreEqual(1000, state.LastUpdated);
                });
        }

        [TestMethod]
        public void TestValueCollectionUnwrap()
        {
            var c = new int[] {}.ToCollection().ToLiveCollection() as ILiveCollection<int>;
            var lc = c.ToLive();

            var f = lc
                .Unwrap()
                .ToIndependent();

            f.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(null, state.Delta);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new int[] { }));
                });

            f.States().Skip(1).Take(1).Check(
                () => lc.PublishValue = new[] { 1, 2, 3 }.ToCollection().ToLiveCollection() as ILiveCollection<int>,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Reconnecting, state.Status);
                    Assert.AreEqual(null, state.Delta);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1, 2, 3 }));
                });

            f.States().Skip(1).Take(1).Check(
                () => lc.PublishValue = LiveCollection<int>.Empty,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Reconnecting, state.Status);
                    Assert.AreEqual(null, state.Delta);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new int[] { }));
                });
        }

        [TestMethod]
        public void TestLiveCollectionUnwrap()
        {
            var x = new[] { 1, 2, 3, 4, 5 }
                .Select(i => i.ToLive(i))
                .ToCollection()
                .ToLiveCollection();
            var w = x.Unwrap().ToIndependent();

            w.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[] { 1, 2, 3, 4, 5 }));
                    Assert.IsTrue(s.Delta == null);
                    Assert.AreEqual(5, s.LastUpdated);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(0.ToLive(500), 1000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[] { 1, 2, 3, 4, 5, 0 }));
                    Assert.IsTrue(s.Delta.Inserts.UnorderedEqual(new[] { 0 }));
                    Assert.IsTrue(s.Delta.Deletes == null);
                    Assert.AreEqual(1000, s.LastUpdated);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(x.ToArray().Single(i => i.PublishValue == 3), 2000),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[] { 1, 2, 4, 5, 0 }));
                    Assert.IsTrue(s.Delta.Inserts == null);
                    Assert.IsTrue(s.Delta.Deletes.UnorderedEqual(new[] { 3 }));
                    Assert.AreEqual(2000, s.LastUpdated);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Connect(new[] { 10.ToLive(3000) }, 2900),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Reconnecting);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[] { 10 }));
                    Assert.IsTrue(s.Delta == null);
                    Assert.AreEqual(3000, s.LastUpdated);
                });
        }

        [TestMethod]
        public void TestCollectionSelectUncached()
        {
            var c = new int[] { }.ToCollection().ToLiveCollection();
            var d = c.SelectStatic(i => i).ToIndependent();

            d.States().Take(1).Check(
                () => { },
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.Inner.Count());
                    Assert.AreEqual(null, state.Delta);

                    Assert.IsFalse(enumerator.MoveNext());
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(1),
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1 }));
                    Assert.IsTrue(state.Delta.Inserts.UnorderedEqual(new[] { 1 }));

                    Assert.IsFalse(enumerator.MoveNext());
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Remove(1),
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new int[0]));
                    Assert.IsTrue(state.Delta.Deletes.UnorderedEqual(new[] { 1 }));

                    Assert.IsFalse(enumerator.MoveNext());
                });
        }

        [TestMethod]
        public void TestCollectionSelectCache()
        {
            var c = new int[] { }.ToCollection().ToLiveCollection();
            var d = c.SelectStatic(i => i).ToIndependent();

            d.States().Take(1).Check(
                () => { },
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.Inner.Count());
                    Assert.AreEqual(null, state.Delta);

                    Assert.IsFalse(enumerator.MoveNext());
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(1),
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1 }));
                    Assert.IsTrue(state.Delta.Inserts.UnorderedEqual(new[] { 1 }));

                    Assert.IsFalse(enumerator.MoveNext());
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Remove(1),
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new int[0]));
                    Assert.IsTrue(state.Delta.Deletes.UnorderedEqual(new[] { 1 }));

                    Assert.IsFalse(enumerator.MoveNext());
                });
        }

        [TestMethod]
        public void TestCollectionGroup()
        {
            var c = new int[] { }.ToCollection().ToLiveCollection();
            var d = c
                .Where(i => i > -1)
                .SelectStatic(e => new KeyValuePair<int, int>(e, e));
            var g = d.Group().ToIndependent();

            g.States().Subscribe(s => { });

            g.States().Take(1).Check(
                () => { },
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.Inner.Count());
                    Assert.AreEqual(null, state.Delta);

                    Assert.IsFalse(enumerator.MoveNext());
                });

            g.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(1),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.Count() == 1);
                    var inner = state.Inner.First();
                    Assert.AreEqual(1, inner.Key);

                    Assert.AreEqual(StateStatus.Connecting, inner.Value.Status());
                    Assert.IsTrue(inner.Value.ToArray().SequenceEqual(new[] { 1 }));

                    var insert = state.Delta.Inserts.Single();
                    Assert.AreEqual(1, insert.Key);
                    Assert.IsTrue(insert.Value.ToArray().SequenceEqual(new[] { 1 }));
                });

            g.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(1),
                results => Assert.AreEqual(0, results.Count()));

            g.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(2),
                results =>
                {
                    var enumerator = results.GetEnumerator();
                    Assert.IsTrue(enumerator.MoveNext());

                    var state = enumerator.Current;
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    var innerEnum = state.Inner.GetEnumerator();
                    Assert.IsTrue(innerEnum.MoveNext());
                    var inner = innerEnum.Current;
                    Assert.AreEqual(StateStatus.Connecting, inner.Value.Status());
                    Assert.AreEqual(1, inner.Key);
                    Assert.IsTrue(inner.Value.ToArray().SequenceEqual(new[] { 1, 1 }));
                    Assert.IsTrue(innerEnum.MoveNext());
                    inner = innerEnum.Current;
                    Assert.AreEqual(StateStatus.Connecting, inner.Value.Status());
                    Assert.AreEqual(2, inner.Key);
                    Assert.IsTrue(inner.Value.ToArray().SequenceEqual(new[] { 2 }));
                    Assert.IsFalse(innerEnum.MoveNext());

                    var insert = state.Delta.Inserts.Single();
                    Assert.AreEqual(2, insert.Key);
                    Assert.IsTrue(insert.Value.ToArray().SequenceEqual(new[] { 2 }));

                    Assert.IsFalse(enumerator.MoveNext());
                });

            g.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(2),
                results => Assert.AreEqual(0, results.Count()));

            g.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Remove(1),
                results => Assert.AreEqual(0, results.Count()));

            g.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Remove(1),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    var innerEnum = state.Inner.GetEnumerator();
                    Assert.IsTrue(innerEnum.MoveNext());
                    var inner = innerEnum.Current;
                    Assert.AreEqual(2, inner.Key);
                    Assert.IsTrue(inner.Value.ToArray().SequenceEqual(new[] { 2, 2 }));
                    Assert.IsFalse(innerEnum.MoveNext());

                    var delete = state.Delta.Deletes.Single();
                    Assert.AreEqual(1, delete.Key);
                    Assert.IsTrue(delete.Value.ToArray().SequenceEqual(new int[0]));
                });
        }

        public class Liquidity
        {
            public readonly Live<decimal?> Price = Live<decimal?>.NewDefault();
            public readonly Live<decimal> Stake = Live<decimal>.NewDefault();
            public override string ToString()
            {
                return string.Format("Price={0} Stake={1}", Price.PublishValue, Stake.PublishValue);
            }
        }

        [TestMethod]
        public void TestCollectionUnsubscribe()
        {
            {
                var c = new[] { 1, 2, 3 }.ToCollection().ToLiveCollection();
                var d = c.ToIndependent();

                var observer = d.CreateObserver(() => { });
                using (d.Subscribe(observer))
                {
                    using (var s = observer.GetState())
                    {
                        Assert.IsTrue(s.Status == StateStatus.Connecting);
                        Assert.IsTrue(s.Inner.UnorderedEqual(new[] { 1, 2, 3 }));
                        Assert.IsTrue(s.Delta == null);
                    }
                }

                using (Publish.Transaction(true))
                {
                    c.PublishInner.Add(0);
                }

                using (var s = observer.GetState())
                {
                    Assert.IsTrue(s.Status == StateStatus.Completing);
                    Assert.IsTrue(s.Inner.UnorderedEqual(new[] { 0, 1, 2, 3 }));
                    Assert.IsTrue(s.Delta.Inserts.UnorderedEqual(new[] { 0 }));
                    Assert.IsTrue(s.Delta.Deletes == null);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        //[TestMethod]
        //public void TestCollectionOrderBy()
        //{
        //    var a = new X { In = { PublishValue = 0 }, S = { PublishValue = "A" } };
        //    var b = new X { In = { PublishValue = 1 }, S = { PublishValue = "B" } };
        //    var c = new X { In = { PublishValue = 2 }, S = { PublishValue = "C" } };
        //    var col = new Collection<X>().ToLiveCollection();

        //    var dic = col.ToLiveDictionaryStatic(s => s.In, s => s);
        //    var x =
        //        dic
        //            .Values()
        //            .OrderBy(s => s.Sub.Select(p => p == null ? int.MaxValue.ToLiveConst() : p.In));

        //    col.TraceAll("col").ToDebug();
        //    dic.TraceAll("dic").ToDebug();
        //    x.TraceAll("x").ToDebug();
        //    col.TraceAll("col1").ToDebug();
        //    dic.TraceAll("dic1").ToDebug();
        //    x.TraceAll("x1").ToDebug();

        //    Observable.Timer(TimeSpan.FromSeconds(5))
        //        .Subscribe(l =>
        //                       {
        //                           using (Publish.Transaction(true))
        //                           {
        //                               col.PublishInner.Add(c);
        //                               col.PublishInner.Add(b);
        //                               col.PublishInner.Add(a);
        //                           }
        //                       });

        //    Thread.Sleep(TimeSpan.FromSeconds(6));
        //}
    }
}
