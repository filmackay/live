using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLiveDictionary
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
        public void TestGroup()
        {
            var source = new[] {0, 1, 1, 2, 3, 3, 3, 4, 5}
                .ToList()
                .ToLiveCollection();

            var lookup = source
                .SelectStatic(e => new KeyValuePair<int, int>(e, e))
                .Group()
                .ToIndependent();

            lookup.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(null, state.Delta);

                    state.Inner
                        .Verify(new[]
                            {
                                KeyValuePair.Create(0, new[] { 0 }),
                                KeyValuePair.Create(1, new[] { 1, 1 }),
                                KeyValuePair.Create(2, new[] { 2 }),
                                KeyValuePair.Create(3, new[] { 3, 3, 3 }),
                                KeyValuePair.Create(4, new[] { 4 }),
                                KeyValuePair.Create(5, new[] { 5 }),
                            });
                });

            lookup.States().Skip(1).Take(1).Check(
                () => source.PublishInner.Remove(4),
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);

                    state.Inner
                        .Verify(new[]
                            {
                                KeyValuePair.Create(0, new[] { 0 }),
                                KeyValuePair.Create(1, new[] { 1, 1 }),
                                KeyValuePair.Create(2, new[] { 2 }),
                                KeyValuePair.Create(3, new[] { 3, 3, 3 }),
                                KeyValuePair.Create(5, new[] { 5 }),
                            });

                    state.Delta
                        .Deletes
                        .Verify(new[]
                            {
                                KeyValuePair.Create(4, new int[0]),
                            });
                });

            lookup.States().Skip(1).Take(1).Check(
                () => source.PublishInner.Remove(3),
                results => Assert.AreEqual(0, results.Count()));
        }

        [TestMethod]
        public void TestLiveDictionaryUnwrap()
        {
            LiveDictionary<int, Live<int>> x =
                new[] {1, 2, 3, 4, 5}
                    .ToDictionary(i => i, i => i.ToLive())
                    .ToLiveDictionary();

            var w = x.Unwrap<int, int, Live<int>>().ToIndependent();

            w.States().Take(1).Check(
                () => { },
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connecting);
                    Assert.IsTrue(s.Inner.UnorderedEqual(
                        new[]
                            {
                                KeyValuePair.Create(1, 1),
                                KeyValuePair.Create(2, 2),
                                KeyValuePair.Create(3, 3),
                                KeyValuePair.Create(4, 4),
                                KeyValuePair.Create(5, 5),
                            }));
                    Assert.IsTrue(s.Delta == null);
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Add(KeyValuePair.Create(0, 0.ToLive())),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Inner.UnorderedEqual(
                        new[]
                            {
                                KeyValuePair.Create(1, 1),
                                KeyValuePair.Create(2, 2),
                                KeyValuePair.Create(3, 3),
                                KeyValuePair.Create(4, 4),
                                KeyValuePair.Create(5, 5),
                                KeyValuePair.Create(0, 0),
                            }));
                    Assert.IsTrue(s.Delta.Inserts.UnorderedEqual(
                        new[]
                            {
                                KeyValuePair.Create(0, 0),
                            }));
                });
            w.States().Skip(1).Take(1).Check(
                () => x.PublishInner.Remove(x.ToArray().Single(i => i.Value.PublishValue == 3)),
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.UnorderedEqual(
                        new[]
                            {
                                KeyValuePair.Create(1, 1),
                                KeyValuePair.Create(2, 2),
                                KeyValuePair.Create(4, 4),
                                KeyValuePair.Create(5, 5),
                                KeyValuePair.Create(0, 0),
                            }));
                    Assert.IsTrue(s.Delta.Deletes.UnorderedEqual(
                        new[]
                            {
                                KeyValuePair.Create(3, 3),
                            }));
                });
            w.States().Skip(1).Take(1).Check(
                () => x.ToArray().Single(i => i.Key == 2).Value.PublishValue = -2,
                state =>
                {
                    var s = state.Single();
                    Assert.IsTrue(s.Status == StateStatus.Connected);
                    Assert.IsTrue(s.Delta.HasChange());
                    Assert.IsTrue(s.Inner.UnorderedEqual(
                        new[]
                            {
                                KeyValuePair.Create(1, 1),
                                KeyValuePair.Create(2, -2),
                                KeyValuePair.Create(4, 4),
                                KeyValuePair.Create(5, 5),
                                KeyValuePair.Create(0, 0),
                            }));
                    Assert.IsTrue(s.Delta.Inserts.UnorderedEqual(
                        new[]
                            {
                                KeyValuePair.Create(2, -2),
                            }));
                    Assert.IsTrue(s.Delta.Deletes.UnorderedEqual(
                        new[]
                            {
                                KeyValuePair.Create(2, 2),
                            }));
                });
        }
    }
}
