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
    public class TestMax
    {
        public TestMax()
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
        public void TestMaxGroupBy()
        {
            var data =
                new[]
                {
                    KeyValuePair.Create(1, "A"),
                }
                .ToList()
                .ToLiveList();

            var a = data
                .Group()
                .MaxByKey();

            a.States().Take(1).Check(
                () => { },
                results =>
                {
                    Assert.AreEqual(1, results.Count());
                    var state = results.First();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(1, state.NewValue.Key);
                    var arr = state.NewValue.Value.ToArray();
                    Assert.IsTrue(arr.UnorderedEqual(new[] { "A" }));
                });

            a.States().Skip(1).Take(1).Check(
                () => data.PublishInner.Add(KeyValuePair.Create(0, "X")),
                results => Assert.AreEqual(0, results.Count()));

            a.States().Skip(1).Take(1).Check(
                () => data.PublishInner.Add(KeyValuePair.Create(2, "B")),
                results =>
                {
                    Assert.AreEqual(1, results.Count());
                    var state = results.First();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.IsTrue(state.HasChange);
                    Assert.AreEqual(2, state.NewValue.Key);
                    Assert.IsTrue(state.NewValue.Value.ToArray().UnorderedEqual(new[] { "B" }));

                    Assert.AreEqual(1, state.OldValue.Key);
                    Assert.IsTrue(state.OldValue.Value.ToArray().UnorderedEqual(new[] { "A" }));
                });
        }
    }
}
