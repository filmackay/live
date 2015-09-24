using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Vertigo.Live;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLiveAggregates
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
        public void TestListMax()
        {
            var l = new List<int?> { 0, 1, 2, 3, 4, 5 }.ToLiveList();

            var max = l.DefaultIfEmpty().Max();
            Assert.AreEqual(5, max.Value);

            max.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(null, state.OldValue);
                    Assert.AreEqual(5, state.NewValue);
                });

            max.States().Skip(1).Take(1).Check(
                () => l.PublishInner[0] = 10,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(5, state.OldValue);
                    Assert.AreEqual(10, state.NewValue);
                });
            Assert.AreEqual(10, max.Value);

            max.States().Skip(1).Take(1).Check(() =>
                l.PublishInner.RemoveAt(0),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(10, state.OldValue);
                    Assert.AreEqual(5, state.NewValue);
                });
            Assert.AreEqual(5, max.Value);

            max.States().Skip(1).Take(1).Check(() =>
                l.PublishInner.RemoveAt(4),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(5, state.OldValue);
                    Assert.AreEqual(4, state.NewValue);
                });
            Assert.AreEqual(4, max.Value);

            max.States().Skip(1).Take(1).Check(() =>
                l.PublishInner.Clear(),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(4, state.OldValue);
                    Assert.AreEqual(null, state.NewValue);
                });
            Assert.IsFalse(max.Value.HasValue);

            var stocks = new List<Stock>
            {
                new Stock
                {
                    Code = {PublishValue = "A"},
                },
                new Stock
                {
                    Code = {PublishValue = "B"},
                },
                new Stock
                {
                    Code = {PublishValue = "C"},
                },
            }.ToLiveList();
            var maxBid = stocks.DefaultIfEmpty().Max(s => s.Bid);

            maxBid.States().Take(1).Check(() =>
                { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(null, state.OldValue);
                    Assert.AreEqual(null, state.NewValue);
                });
            Assert.AreEqual(false, maxBid.Value.HasValue);

            maxBid.States().Skip(1).Take(1).Check(() =>
                stocks.PublishInner[0].Bid.PublishValue = 100,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(null, state.OldValue);
                    Assert.AreEqual(100, state.NewValue);
                });
            Assert.AreEqual(100, maxBid.Value);
        }

        [TestMethod]
        public void TestListMaxByKey()
        {
            var l = new List<int> {0, 1, 1, 2, 3, 4, 5}.ToLiveList();

            var groups = l.GroupBy(x => x.ToLiveConst());
            ILiveValue<KeyValuePair<int, ILiveCollection<int>>> maxGroup = groups.MaxByKey();

            maxGroup.SelectStatic(kv => kv.Key).Trace("key").ToDebug();
            maxGroup.Select(kv => kv.Value).TraceAll("members").ToDebug();

            l.PublishInner.Add(-1);
            l.PublishInner.Add(6);
            l.PublishInner.Remove(5);
            l.PublishInner.Add(6);
            l.PublishInner.Remove(6);
            l.PublishInner.Remove(6);

            l.PublishInner.Add(-1);
            l.PublishInner.Remove(-1);
            l.PublishInner.Remove(-1);
        }
    }
}
