using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>

    [TestClass]
    public class TestLive
    {
        public TestLive()
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
        public void TestLiveBasics()
        {
            var a = (-1).ToLive(0);
            a.Trace("a");

            a.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.IsFalse(state.HasChange);
                    Assert.AreEqual(-1, state.NewValue);
                    Assert.AreEqual(0, state.LastUpdated);
                });

            a.States().Skip(1).Take(1).Check(
                () => a.SetValue(1, 1000),
                results =>
                {
                    var r = results.ToArray();
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.HasChange);
                    Assert.AreEqual(-1, state.OldValue);
                    Assert.AreEqual(1000, state.LastUpdated);
                });

            a.States().Skip(1).Take(1).Check(
                () => a.SetValue(2, 2000),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.HasChange);
                    Assert.AreEqual(1, state.OldValue);
                    Assert.AreEqual(2, state.NewValue);
                    Assert.AreEqual(2000, state.LastUpdated);
                });
        }

        [TestMethod]
        public void TestLiveReconnect()
        {
            var a = 0.ToLive();
            a.Trace("a");

            a.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.IsFalse(state.HasChange);
                    Assert.AreEqual(0, state.NewValue);
                });

            a.States().Skip(1).Take(1).Check(
                () => a.PublishValue = 1,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.HasChange);
                    Assert.AreEqual(0, state.OldValue);
                    Assert.AreEqual(1, state.NewValue);
                });

            a.States().Skip(1).Take(1).Check(
                () => a.Connect(2),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.HasChange);
                    Assert.AreEqual(1, state.OldValue);
                    Assert.AreEqual(2, state.NewValue);
                });

            a.Check1(
                () =>
                {
                    a.PublishValue = -1;
                    a.Disconnect();
                    a.Connect(-2);
                    a.Disconnect();
                    a.Connect(3);
                },
                results =>
                {
                    var state = results.Skip(1).Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.HasChange);
                    Assert.AreEqual(2, state.OldValue);
                    Assert.AreEqual(3, state.NewValue);
                });
        }

        [TestMethod]
        public void TestLiveComplete()
        {
            var a = 0.ToLive();
            a.Trace("a");

            a.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.IsFalse(state.HasChange);
                    Assert.AreEqual(0, state.NewValue);
                });

            a.States().Skip(1).Take(1).Check(
                () =>
                {
                    a.PublishValue = 1;
                    a.Complete();
                },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Completing, state.Status);
                    Assert.IsTrue(state.HasChange);
                    Assert.AreEqual(0, state.OldValue);
                    Assert.AreEqual(1, state.NewValue);
                });
        }

        class C : INotifyPropertyChanged
        {
            private int _prop1;
            public int Prop1
            {
                get { return _prop1; }
                set
                {
                    _prop1 = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("Prop1"));
                }
            }

            #region Implementation of INotifyPropertyChanged

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion
        }

        [TestMethod]
        public void TestProperty()
        {
            var a = 5.ToLive();
            var b = 3.ToLive();
            var c = new C { Prop1 = 1 };
            var prop1 = c.LiveProperty<int>("Prop1");

            var x = a.Add(b).Add(prop1);
            var xo = x.CreateObserver(o =>
                Publish.OnConsume(
                    () =>
                    {
                        var change = o.GetState();
                        Debug.Print("{0}: {1}", change.Status, change.NewValue);
                    }));
            x.Subscribe(xo);

            Assert.AreEqual(9, x.Value);

            b.PublishValue = 10;
            Assert.AreEqual(16, x.Snapshot.NewValue);

            c.Prop1 = 2;
            Assert.AreEqual(x.Snapshot.NewValue, 17);
        }

        static void CheckArithmetic<T>(Func<ILiveValue<T>, ILiveValue<T>, ILiveValue<T>> liveOperation, Func<T, T, T> operation, T[] left, T[] right)
        {
            var lleft = Live<T>.NewDefault();
            var lright = Live<T>.NewDefault();
            var lresult = liveOperation(lleft, lright);

            for (var index = 0; index < left.Length; index++)
            {
                using (Publish.Transaction(true))
                {
                    lright.PublishValue = right[index];
                    lleft.PublishValue = left[index];
                }

                var result = default(T);
                try
                {
                    result = operation(lleft.Value, lright.Value);
                }
                catch
                {
                }
                Assert.AreEqual(result, lresult.Snapshot.NewValue);
            }
        }

        [TestMethod]
        public void TestArithmetic()
        {
            CheckArithmetic((a, b) => a.Add(b), (a, b) => (a + b), new[] { 1, 2, 3 }, new[] { -7, 8, -9 });
            CheckArithmetic((a, b) => a.Subtract(b), (a, b) => (a - b), new[] { 1, 2, 3 }, new[] { -7, 8, -9 });
            CheckArithmetic((a, b) => a.Multiply(b), (a, b) => (a * b), new[] { 1, 2, 3 }, new[] { -7, 8, -9 });
            //CheckArithmetic((a, b) => a.Divide(b), (a, b) => (a / b), new[] { 1, 2, 3, 5 }, new[] { -7, 8, -9, 0 });
        }

        [TestMethod]
        public void TestLiveValue()
        {
            var d = 0.ToLive();

            d.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.NewValue);
                });
            Assert.AreEqual(0, d.Snapshot.NewValue);

            d.States().Skip(1).Take(1).Check(
                () => d.PublishValue = 1,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(0, state.OldValue);
                    Assert.AreEqual(1, state.NewValue);
                });
            Assert.AreEqual(1, d.Snapshot.NewValue);

            d.States().Skip(1).Take(1).Check(
                () => d.PublishValue = 1,
                results =>
                {
                    Assert.AreEqual(results.Count(), 0);
                });
            Assert.AreEqual(1, d.Snapshot.NewValue);
        }

        [TestMethod]
        public void TestLiveValueUnwrap()
        {
            var d = 0.ToLive();
            var w = d.ToLive();
            var u = w.Unwrap();

            u.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.NewValue);
                });
            Assert.AreEqual(0, d.Snapshot.NewValue);

            u.States().Skip(1).Take(1).Check(
                () => d.PublishValue = 1,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(0, state.OldValue);
                    Assert.AreEqual(1, state.NewValue);
                });
            Assert.AreEqual(1, d.Snapshot.NewValue);
        }

        [TestMethod]
        public void Test1()
        {
            var a = ((int?)0).ToLive();
            var d = a.NullSelectStatic(i => i, -1);

            d.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.NewValue);
                });
            Assert.AreEqual(0, d.Snapshot.NewValue);

            d.States().Skip(1).Take(1).Check(
                () => a.PublishValue = 1,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(0, state.OldValue);
                    Assert.AreEqual(1, state.NewValue);
                });
            Assert.AreEqual(1, d.Snapshot.NewValue);

            d.States().Skip(1).Take(1).Check(
                () => a.PublishValue = null,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(1, state.OldValue);
                    Assert.AreEqual(-1, state.NewValue);
                });
            Assert.AreEqual(-1, d.Snapshot.NewValue);
        }

        [TestMethod]
        public void Test2()
        {
            var b = new[] { 0 }.ToCollection().ToLiveCollection();
            var c = new[] { 1 }.ToCollection().ToLiveCollection();
            var lc = c.ToLive();
            var d = lc.Unwrap().ToIndependent();

            d.Trace("d1");
            d.Trace("d2");

            d.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { 1 }));
                    Assert.AreEqual(null, state.Delta);
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Remove(1),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new int[0]));
                    Assert.IsTrue(state.Delta.HasChange());
                    Debug.Assert(state.Delta.Deletes.UnorderedEqual(new[] { 1 }));
                });

            d.States().Skip(1).Take(1).Check(
                () => c.PublishInner.Add(1),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new [] { 1 }));
                    Assert.IsTrue(state.Delta.HasChange());
                    Debug.Assert(state.Delta.Inserts.UnorderedEqual(new[] { 1 }));
                });

            d.States().Skip(1).Take(1).Check(
                () => lc.Connect(b),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Reconnecting, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { 0 }));
                    Assert.IsFalse(state.Delta.HasChange());
                });
        }

        [TestMethod]
        public void Test3()
        {
            var a = new X { I = { PublishValue = null }, S = { PublishValue = "A" }};
            var b = new X { I = { PublishValue = 2 }, S = { PublishValue = "B" } };
            var c = new Collection<X>().ToLiveCollection();

            var d1 = c
                .Where(x => x.I.HasValue())
                .GroupBy(x => x.I.GetValueOrDefault())
                .DefaultIfEmpty()
                .MaxByKey()
                .Select(g => g.Value);
            var d = d1
                .OrderBy(x => x.S)
                .ToIndependent();

            d.TraceAll("d").ToDebug();
            d1.Trace("d1").ToDebug();

            d.States().Take(1).Check(
                () => { },
                results => Assert.AreEqual(0, results.Count()));

            d.States().Take(1).Check(
                () => c.PublishInner.Add(a),
                results => Assert.AreEqual(0, results.Count()));

            d.States().Take(1).Check(
                () => c.PublishInner.Add(b),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { b }));
                    Assert.IsFalse(state.Delta.HasChange());
                });

            d.States().Skip(1).Take(1).Check(
                () => a.I.PublishValue = 2,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { a, b }));
                    Assert.IsTrue(state.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<X>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<X>
                            {
                                InsertItems = new[] { a },
                            },
                        },
                    }, new IndexNodeComparer<X>()));
                });

            d.States().Skip(1).Take(1).Check(
                () => b.I.PublishValue = 3,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Reconnecting, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { b }));
                    Assert.IsFalse(state.Delta.HasChange());
                });

            d.Check1(
                () =>
                {
                    b.I.PublishValue = 1;
                    a.I.PublishValue = 0;
                },
                results =>
                {
                    var state = results.Skip(1).Single();
                    Assert.AreEqual(StateStatus.Reconnecting, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { b }));
                    Assert.IsFalse(state.Delta.HasChange());
                });

            d.Check1(
                () =>
                {
                    b.I.PublishValue = 2;
                    a.I.PublishValue = 2;
                },
                results =>
                {
                    var state = results.Skip(1).Single();
                    Assert.AreEqual(StateStatus.Reconnecting, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { a, b }));
                    Assert.IsFalse(state.Delta.HasChange());
                });

            d.States().Skip(1).Take(1).Check(
                () => b.I.PublishValue = 1,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { a }));
                    Assert.IsTrue(state.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<X>
                        {
                            Index = 1,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<X>
                            {
                                DeleteItems = new[] { b },
                            },
                        },
                    }, new IndexNodeComparer<X>()));
                });

            d.States().Skip(1).Take(1).Check(
                () => a.I.PublishValue = 3,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Reconnecting, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { a }));
                    Assert.IsFalse(state.Delta.HasChange());
                });
        }
    }
}
