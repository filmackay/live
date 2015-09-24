using System;
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
    public class TestLiveValue
    {
        public TestLiveValue()
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
        public void TestLiveValueRange()
        {
            var start = 10.ToLive(0);
            var count = 3.ToLive(0);
            var r = LiveValue.Range(start, count).ToIndependent();

            r.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.IsFalse(state.Delta.HasChange());
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { 10, 11, 12 }));
                    Assert.AreEqual(0, state.LastUpdated);
                });

            r.States().Skip(1).Take(1).Check(
                () => start.SetValue(20, 100),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { 20, 21, 22 }));
                    Assert.AreEqual(100, state.LastUpdated);
                    Assert.IsTrue(state.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 10, 11, 12 },
                                InsertItems = new[] { 20, 21, 22 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            r.States().Skip(1).Take(1).Check(
                () => start.SetValue(10, 200),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { 10, 11, 12 }));
                    Assert.AreEqual(200, state.LastUpdated);
                    Assert.IsTrue(state.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 0,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 10, 11, 12 },
                                DeleteItems = new[] { 20, 21, 22 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            r.States().Skip(1).Take(1).Check(
                () => count.SetValue(5, 300),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { 10, 11, 12, 13, 14 }));
                    Assert.AreEqual(300, state.LastUpdated);
                    Assert.IsTrue(state.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 3,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                InsertItems = new[] { 13, 14 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });

            r.States().Skip(1).Take(1).Check(
                () => count.SetValue(3, 400),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.SequenceEqual(new[] { 10, 11, 12 }));
                    Assert.AreEqual(400, state.LastUpdated);
                    Assert.IsTrue(state.Delta.IndexDeltas.SequenceEqual(new[]
                    {
                        new IndexNode<int>
                        {
                            Index = 3,
                            DenseIndex = 0,
                            Data = new ListIndexDelta<int>
                            {
                                DeleteItems = new[] { 13, 14 },
                            },
                        },
                    }, new IndexNodeComparer<int>()));
                });
        }

        [TestMethod]
        public void TestLiveValueDistinct()
        {
            var x = new Live<int>(0, 0);
            var r = x.DistinctUntilChanged();

            r.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.NewValue);
                    Assert.AreEqual(0, state.LastUpdated);
                });

            r.States().Skip(1).Take(1).Check(
                () => x.SetValue(0, 100),
                results => Assert.AreEqual(0, results.Count()));

            r.States().Skip(1).Take(1).Check(
                () => x.SetValue(1, 200),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(1, state.NewValue);
                    Assert.AreEqual(0, state.OldValue);
                    Assert.AreEqual(200, state.LastUpdated);
                });
        }
    }
}
