using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLiveSet
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
        public void TestSetStock()
        {
            var oldStocks = new HashSet<OldStock>(new[]
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

            var oOldStocks = oldStocks.ToLiveSet();
            var iOldStocks = oOldStocks.PublishInner;
            var totalPrices = oOldStocks.Select(s => s.LiveProperty<decimal>("Price")).Sum();

            var stocks = new HashSet<Stock>
            {
                new Stock()
                {
                    Code = {PublishValue = "ABC" },
                    Name = {PublishValue = "ABC Limited"},
                    Price = {PublishValue = 100},
                    Turnover = {PublishValue = 1000},
                    Bid = {PublishValue = 10},
                    Ask = {PublishValue = 12},
                },
                new Stock()
                {
                    Code = {PublishValue = "DEF"},
                    Name = {PublishValue = "DEF Limited"},
                    Price = {PublishValue = 20},
                    Turnover = {PublishValue = 100},
                    Bid = {PublishValue = 8},
                    Ask = {PublishValue = 11},
                },
            }.ToLiveSet();

            var turnovers = stocks.Select(s => s.Turnover);
            var totalTurnover = turnovers.Sum();
            totalTurnover.Trace("totalTurnover");
            using (Publish.Transaction(true))
            {
            }

            Assert.AreEqual(StateStatus.Connecting, totalTurnover.Snapshot.Status);
            Assert.AreEqual(totalTurnover.Value, 1100);

            var abc = stocks.PublishInner.Single(s => s.Code.PublishValue == "ABC");
            var def = stocks.PublishInner.Single(s => s.Code.PublishValue == "DEF");

            using (Publish.Transaction(true))
            {
                abc.Turnover.PublishValue = 999;
                Assert.AreEqual(1100, totalTurnover.Snapshot.NewValue);
            }

            using (Publish.Transaction(true))
            {
                Assert.AreEqual(1099, totalTurnover.Snapshot.NewValue);
                abc.Turnover.PublishValue = 997;
                abc.Turnover.PublishValue = 998;
                Assert.AreEqual(1099, totalTurnover.Snapshot.NewValue);
            }

            using (Publish.Transaction(true))
            {
                Assert.AreEqual(1098, totalTurnover.Snapshot.NewValue);

                var overlap = stocks.CrossJoin(stocks).Where(ab => ab.Item1.Bid.GreaterThan(ab.Item2.Ask));
                overlap.Trace("overlap");
            }

            using (Publish.Transaction(true))
            {
                abc.Bid.PublishValue = 11.5M;
            }
            using (Publish.Transaction(true))
            {
                def.Ask.PublishValue = 12;
            }
        }

        [TestMethod]
        public void TestSetLiveWhere()
        {
            var x = new[] { 1 }
                .Select(i => i.ToLive())
                .ToHashSet()
                .ToLiveSet();
            var w = x.Where(i => i.LessThanOrEqual(3.ToLiveConst())).ToIndependent();

            w.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.Select(i => i.Value).UnorderedEqual(new[] { 1 }));
                    Assert.IsTrue(s.Delta == null);
                });

            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(0.ToLive()),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.Select(i => i.Value).UnorderedEqual(new[] { 0, 1 }));
                    Assert.IsTrue(s.Delta.Inserts.Select(i => i.Value).UnorderedEqual(new[] { 0 }));
                    Assert.IsTrue(s.Delta.Deletes == null);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(10.ToLive()),
                results => Assert.AreEqual(0, results.Count()));

            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(x.PublishInner.Single(i => i.PublishValue == 1)),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.Select(i => i.Value).UnorderedEqual(new[] { 0 }));
                    Assert.IsTrue(s.Delta.Inserts == null);
                    Assert.IsTrue(s.Delta.Deletes.Select(i => i.Value).UnorderedEqual(new[] { 1 }));
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(x.PublishInner.Single(i => i.PublishValue == 0)),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.Select(i => i.Value).UnorderedEqual(new int[0]));
                    Assert.IsTrue(s.Delta.Inserts == null);
                    Assert.IsTrue(s.Delta.Deletes.Select(i => i.Value).UnorderedEqual(new[] { 0 }));
                });
        }

        [TestMethod]
        public void TestSetToDictionary()
        {
            var x = new[] {1}
                .ToHashSet()
                .ToLiveSet();
            var w = x.SelectDictionary(k => k.ToLive());

            x.Trace("x");
            w.Trace("w");

            x.PublishInner.Add(2);
            x.PublishInner.Add(3);
        }
    }
}
