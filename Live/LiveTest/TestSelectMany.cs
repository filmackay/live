using System.Linq;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestSelectMany
    {
        public TestSelectMany()
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
        public void TestSelectManySimple()
        {
            var source = new[]
                {
                    new[] { 1, 2, 3 }.ToList().ToLiveList(),
                    new[] { 4, 5, 6 }.ToList().ToLiveList(),
                }.ToList().ToLiveList();
            var a = source.PublishInner[0];
            var b = source.PublishInner[1];

            var all = source.SelectMany(s => s).ToIndependent();

            all.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(null, state.Delta);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1, 2, 3, 4, 5, 6 }));
                });

            all.States().Skip(1).Take(1).Check(
                () => a.PublishInner.Add(-1),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1, 2, 3, 4, 5, 6, -1 }));

                    state.Delta.Inserts.UnorderedEqual(new[] { -1 });
                });

            all.States().Skip(1).Take(1).Check(
                () => a.PublishInner.Remove(-1),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.Inner.UnorderedEqual(new[] { 1, 2, 3, 4, 5, 6 }));

                    state.Delta.Deletes.UnorderedEqual(new[] { -1 });
                });
        }
    }
}
