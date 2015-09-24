using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vertigo.Live.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestLocks
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
        public void TestNotifyLock()
        {
            var onNotify = 0;
            var notify = 0;
            var process = 0;
            NotifyLock notifyLock = null;
            notifyLock = new NotifyLock
            {
                OnNotify = () =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        onNotify++;
                        notifyLock.Process(notified =>
                        {
                            if (notified)
                                Interlocked.Increment(ref process);
                        });
                    });
                },
            };

            var tasks = new List<Task>();
            for (var i = 0; i < 10000000; i++)
            {
                if (notifyLock.Notify())
                {
                    tasks.Add(Task.Factory.StartNew(() => Interlocked.Increment(ref notify)));

                    if (tasks.Count > 1000)
                    {
                        Task.WaitAll(tasks.ToArray());
                        tasks.Clear();
                    }
                }
            }

            Task.WaitAll(tasks.ToArray());
            Thread.Sleep(100);
            Debug.Assert(notify == process);
            Debug.Assert(notify == onNotify);
        }
    }
}
