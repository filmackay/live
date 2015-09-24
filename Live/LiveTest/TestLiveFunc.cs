using System;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLiveFunc
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
        public class Liquidity
        {
            public Liquidity()
            {
                AvailablePrice = _ExecutingPrice.OnNull(_PublishedPrice);
            }

            private readonly Live<decimal?> _PublishedPrice = Live<decimal?>.NewDefault();
            public Live<decimal?> PublishedPrice { get { return _PublishedPrice; } }

            private readonly Live<decimal?> _ExecutingPrice = Live<decimal?>.NewDefault();
            public Live<decimal?> ExecutingPrice { get { return _ExecutingPrice; } }

            private readonly Live<decimal> _PublishedStake = Live<decimal>.NewDefault();
            public Live<decimal> PublishedStake { get { return _PublishedStake; } }

            public ILiveValue<decimal?> AvailablePrice { get; private set; }
            public ILiveValue<decimal> AvailableStake { get { return _PublishedStake; } }

            public void Clear()
            {
                _PublishedStake.PublishValue = 0;
                _PublishedPrice.PublishValue = null;
                _ExecutingPrice.PublishValue = null;
            }
        }

        [TestMethod]
        public void TestOnNull()
        {
            var l = new Liquidity();

            l.AvailablePrice.States().Take(1).Check(() =>
                { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(null, state.OldValue);
                    Assert.AreEqual(null, state.NewValue);
                });

            l.AvailablePrice.States().Skip(1).Take(1).Check(() =>
                l.PublishedPrice.PublishValue = 3,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(null, state.OldValue);
                    Assert.AreEqual(3, state.NewValue);
                });
        }

        [TestMethod]
        public void TestLiveFuncSimple()
        {
            var f = LiveFunc.Create<int, int, int>((a, b) => a + b);

            var x = 0.ToLive();
            var y = 0.ToLive();

            var z = f(x, y);

            z.States().Take(1).Check(() =>
            { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.NewValue);
                });

            z.States().Skip(1).Take(1).Check(
                () => x.PublishValue = 1,
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(0, state.OldValue);
                    Assert.AreEqual(1, state.NewValue);
                });

            z.States().Skip(1).Take(1).Check(
                () =>
                    {
                        x.PublishValue = 1;
                        y.PublishValue = 10;
                    },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(1, state.OldValue);
                    Assert.AreEqual(11, state.NewValue);
                });
        }
    }
}
