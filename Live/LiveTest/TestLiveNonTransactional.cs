using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLiveNonTransactional
    {
        public TestLiveNonTransactional()
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
            var a = new Live<int>(-1) { Transactional = false };
            a.Trace("a");

            a.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.IsFalse(state.HasChange);
                    Assert.AreEqual(-1, state.NewValue);
                });

            a.States().Check(
                () => a.SetValue(1, 1000),
                results =>
                {
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
    }
}
