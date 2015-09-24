using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestGroupBy
    {
        public TestGroupBy()
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

        public class A
        {
            public Live<int> I = Live<int>.NewDefault();
        }

        [TestMethod]
        public void TestLiveSetGroupBy()
        {
            var data = Enumerable
                .Range(0, 3)
                .Select(i => new A())
                .ToHashSet()
                .ToLiveSet();

            var a = data
                .GroupBy(i => i.I)
                .ToIndependent();

            a.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(1, state.Inner.Count());
                    var group = state.Inner.Single();
                    Assert.AreEqual(group.Key, 0);

                    Assert.IsTrue(group.Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 0, 0, 0 }));
                    Assert.IsNull(state.Delta);
                });

            a.States().Skip(1).Take(1).Check(
                () =>
                {
                    data.PublishInner.First().I.PublishValue = 1;
                },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(2, state.Inner.Count());
                    var groups = state.Inner.ToArray();

                    Assert.AreEqual(groups[0].Key, 0);
                    Assert.IsTrue(groups[0].Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 0, 0 }));

                    Assert.AreEqual(groups[1].Key, 1);
                    Assert.IsTrue(groups[1].Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 1 }));

                    Assert.IsTrue(state.Delta.Deletes == null);
                    var insert = state.Delta.Inserts.Single();
                    Assert.AreEqual(1, insert.Key);
                    Assert.IsTrue(insert.Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 1 }));
                });

            a.States().Skip(1).Take(1).Check(
                () =>
                {
                    data.PublishInner.Skip(1).First().I.PublishValue = 1;
                },
                results => Assert.AreEqual(0, results.Count()));

            a.States().Skip(1).Take(1).Check(
                () =>
                {
                    data.PublishInner.Skip(2).First().I.PublishValue = 1;
                },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(1, state.Inner.Count());
                    var groups = state.Inner.ToArray();
                    Assert.AreEqual(groups[0].Key, 1);
                    Assert.IsTrue(groups[0].Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 1, 1, 1 }));

                    Assert.IsNull(state.Delta.Inserts);
                    var delete = state.Delta.Deletes.Single();
                    Assert.AreEqual(0, delete.Key);
                });
        }

        [TestMethod]
        public void TestLiveListGroupBy()
        {
            var data = Enumerable
                .Range(0, 3)
                .Select(i => new A())
                .ToList()
                .ToLiveList();

            var a = data
                .GroupBy(i => i.I)
                .ToIndependent();

            a.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(1, state.Inner.Count());
                    var group = state.Inner.Single();
                    Assert.AreEqual(group.Key, 0);
                    Assert.IsTrue(group.Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 0, 0, 0 }));
                    Assert.IsNull(state.Delta);
                });

            a.States().Skip(1).Take(1).Check(
                () =>
                {
                    data.PublishInner[0].I.PublishValue = 1;
                },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(2, state.Inner.Count());
                    var groups = state.Inner.ToArray();
                    Assert.AreEqual(groups[0].Key, 0);
                    Assert.IsTrue(groups[0].Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 0, 0 }));

                    Assert.AreEqual(groups[1].Key, 1);
                    Assert.IsTrue(groups[1].Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 1 }));

                    Assert.IsNull(state.Delta.Deletes);
                    var insert = state.Delta.Inserts.Single();
                    Assert.AreEqual(1, insert.Key);
                    Assert.IsTrue(insert.Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 1 }));
                });

            a.States().Skip(1).Take(1).Check(
                () =>
                {
                    data.PublishInner[1].I.PublishValue = 1;
                },
                results => Assert.AreEqual(0, results.Count()));

            a.States().Skip(1).Take(1).Check(
                () =>
                {
                    data.PublishInner[2].I.PublishValue = 1;
                },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(1, state.Inner.Count());
                    var groups = state.Inner.ToArray();
                    Assert.AreEqual(groups[0].Key, 1);
                    Assert.IsTrue(groups[0].Value.ToArray().Select(i => i.I.Value).UnorderedEqual(new[] { 1, 1, 1 }));

                    Assert.IsNull(state.Delta.Inserts);
                    var delete = state.Delta.Deletes.Single();
                    Assert.AreEqual(0, delete.Key);
                });
        }

        [TestMethod]
        public void TestLiveMax()
        {
            var data = Enumerable
                .Range(0, 3)
                .Select(i => new A())
                .ToList()
                .ToLiveList();

            var a = data
                .GroupBy(i => i.I)
                .MaxByKey();

            a.States().Take(1).Check(
                () => { },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connecting, state.Status);
                    Assert.AreEqual(0, state.NewValue.Key);
                });

            a.States().Skip(1).Take(1).Check(
                () =>
                {
                    data.PublishInner[0].I.PublishValue = 1;
                },
                results =>
                {
                    var state = results.Single();
                    Assert.AreEqual(StateStatus.Connected, state.Status);
                    Assert.AreEqual(1, state.NewValue.Key);
                    Assert.AreEqual(0, state.OldValue.Key);
                });

            a.States().Skip(1).Take(1).Check(
                () => data.PublishInner[1].I.PublishValue = 1,
                results => Assert.AreEqual(0, results.Count()));

            a.States().Skip(1).Take(1).Check(
                () => data.PublishInner[2].I.PublishValue = 1,
                results => Assert.AreEqual(0, results.Count()));
        }

        [TestMethod]
        public void TestLiveListGroupBy2()
        {
            var data =
                new List<A>()
                    .ToLiveList();
            data.TraceAll("data").ToDebug();

            using (Publish.Transaction(true))
            {
                Enumerable.Range(0, 7)
                    .ForEach(i =>
                    {
                        var aa = new A();
                        aa.I.Init(i % 5, 0);
                        data.PublishInner.Add(aa);
                    });
            }

            var a = data
                .GroupBy(i => i.I)
                .ToIndependent();
            a.TraceAll("a").ToDebug();
        }
    }
}
