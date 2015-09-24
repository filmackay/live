using System;
using System.Diagnostics;
using System.Threading;
using System.Reactive.Disposables;

//#define READWRITELOCK_TRACE

namespace Vertigo.Live
{
    public enum ReadWriteLockStatus
    {
        Write,
        Read,
        None
    }

    public class ReadWriteLockable
    {
        internal int _counter;
        internal int _writeThreadID;    // for debug information onlys
        public string Name;
        private readonly object _parent;

        public ReadWriteLockable(object parent)
        {
            _parent = parent;
        }

        public int CurrentWriteCount
        {
            get { return _counter.WriterCount(); }
        }

        public int CurrentReadCount
        {
            get { return _counter.ReaderCount(); }
        }

        public ReadWriteLockStatus Status
        {
            get
            {
                if (_counter.WriterCount() > 0)
                    return ReadWriteLockStatus.Write;
                if (_counter.ReaderCount() > 0)
                    return ReadWriteLockStatus.Read;
                return ReadWriteLockStatus.None;
            }
        }

        public IDisposable ReadLock(bool block = true)
        {
            var num2 = 0;
            while (true)
            {
                var oldCounter = Concurrency.Atomic(ref _counter,
                    counter =>
                    {
                        if (counter.WriterCount() > 0)
                            return counter;
                        return counter.SetReaderCount(counter.ReaderCount() + 1);
                    });

                // did we get the lock?
                if (oldCounter.WriterCount() == 0)
                {
#if READWRITELOCK_TRACE
                    Debug.Print("{0} #{1} take ReadLock", GetHashCode(), Thread.CurrentThread.ManagedThreadId);
#endif
                    return Disposable.Create(ReleaseReadLock);
                }

                // there is a blocking writer
                if (!block)
                    return null;

                Locks.Spin(num2++);
            }
        }

        internal void ReleaseReadLock()
        {
            var oldCounter = Concurrency.Atomic(ref _counter, counter => counter.SetReaderCount(counter.ReaderCount() - 1));
            if (oldCounter.ReaderCount() == 0)
                throw new InvalidOperationException("No read lock held");
#if READWRITELOCK_TRACE
            else
                Debug.Print("{0} #{1} release ReadLock", GetHashCode(), Thread.CurrentThread.ManagedThreadId);
#endif
        }

        public Lock WriteLock(bool block = true)
        {
            var num2 = 0;
            while (true)
            {
                // get write lock if possible
                var recursive = _counter.WriterCount() > 0 && _writeThreadID == Thread.CurrentThread.ManagedThreadId;
                Debug.Assert(!recursive);
                var oldCounter = Concurrency.Atomic(ref _counter,
                    counter =>
                        {
                            if (counter == 0)
                                return 0.SetWriterCount(1);
                            return counter;
                        });

                // did we get the lock?
                if (oldCounter == 0)
                {
                    _writeThreadID = Thread.CurrentThread.ManagedThreadId;
#if READWRITELOCK_TRACE
                    Debug.Print("{0} #{1} take WriteLock", GetHashCode(), Thread.CurrentThread.ManagedThreadId);
#endif
                    return new Lock(this, ReadWriteLockStatus.Write);
                }

                // there is a blocking reader or writer
                if (!block)
                    return null;

                // wait
                Locks.Spin(num2++);
            }
        }

        //public IDisposable WriteOrReadLock(bool block = true)
        //{
        //    if (_writeThreadID == Thread.CurrentThread.ManagedThreadId)
        //        throw new InvalidOperationException("Cannot issue more than one write lock");

        //    var num2 = 0;
        //    while (true)
        //    {
        //        // get write lock if possible
        //        var oldCounter = Concurrency.Atomic(ref _counter,
        //            counter =>
        //            {
        //                // write lock already taken? we cant do anything
        //                if (counter.WriterCount() > 0)
        //                    return counter;

        //                // read lock already taken? take a read lock
        //                if (counter.ReaderCount() > 0)
        //                    return counter.SetReaderCount(counter.ReaderCount() + 1);

        //                // no lock taken - take a write lock
        //                return counter.SetWriterCount(counter.WriterCount() + 1);
        //            });

        //        // did we get a lock?
        //        if (oldCounter.WriterCount() > 0)
        //        {
        //            // there is a blocking writer
        //            if (!block)
        //                return null;
        //        }
        //        else if (oldCounter.ReaderCount() > 0)
        //        {
        //            // got a read lock
        //            return new ReadWriteLock(this, ReadWriteLockStatus.Read);
        //        }
        //        else
        //        {
        //            // got a write lock
        //            _writeThreadID = Thread.CurrentThread.ManagedThreadId;
        //            return new ReadWriteLock(this, ReadWriteLockStatus.Write);
        //        }

        //        // wait
        //        Lock.Spin(num2++);
        //    }
        //}

        public class Lock : IDisposable
        {
            private readonly ReadWriteLockable _lockable;
            private ReadWriteLockStatus _status;

            internal Lock(ReadWriteLockable lockable, ReadWriteLockStatus status)
            {
                _lockable = lockable;
                _status = status;
            }

            public void Downgrade()
            {
                // update status
                switch (_status)
                {
                    case ReadWriteLockStatus.Read:
                        // already downgraded
                        return;
                    case ReadWriteLockStatus.None:
                        throw new InvalidOperationException("Cannot downgrade lock that has been released");
                }

                // switch write lock to read lock
                _lockable._writeThreadID = 0;
                Concurrency.Atomic(ref _lockable._counter, counter => 0.SetReaderCount(counter.ReaderCount() + 1).SetWriterCount(counter.WriterCount() - 1));
                _status = ReadWriteLockStatus.Read;
            }

            public bool ReadLock
            {
                get { return _status == ReadWriteLockStatus.Read; }
            }

            public bool WriteLock
            {
                get { return _status == ReadWriteLockStatus.Write; }
            }

            public void Dispose()
            {
                // release lock
                switch (_status)
                {
                    case ReadWriteLockStatus.Write:
                        {
                            var oldCounter =
                                Concurrency.Atomic(
                                    ref _lockable._counter,
                                    counter => counter.SetWriterCount(counter.WriterCount() - 1).SetReaderCount(counter.ReaderCount()));
                            Debug.Assert(oldCounter.WriterCount() > 0);
                            
                            if (oldCounter.WriterCount() > 0)
                            {
#if READWRITELOCK_TRACE
                                Debug.Print("{0} #{1} release WriteLock", GetHashCode(), Thread.CurrentThread.ManagedThreadId);
#endif
                                _lockable._writeThreadID = 0;
                            }
                            else
                                throw new InvalidOperationException("No write lock is held");
                            _status = ReadWriteLockStatus.None;
                        }
                        break;

                    case ReadWriteLockStatus.Read:
                        {
                            var oldCounter =
                                Concurrency.Atomic(
                                    ref _lockable._counter,
                                    counter => counter.SetReaderCount(counter.ReaderCount() - 1).SetWriterCount(counter.WriterCount()));
                            if (oldCounter.ReaderCount() > 0)
                            {
#if READWRITELOCK_TRACE
                                Debug.Print("{0} #{1} release ReadLock", GetHashCode(), Thread.CurrentThread.ManagedThreadId);
#endif
                            }
                            else
                                throw new InvalidOperationException("No read lock is held");
                        }
                        break;

                    default:
                        throw new InvalidOperationException("No lock is held");
                }

                GC.SuppressFinalize(this);
            }
        }
    }

    public static partial class Extensions
    {
        public static int ReaderCount(this int number)
        { return number & 0x0000FFFF; }

        public static int SetReaderCount(this int number, int newValue)
        {
            unchecked
            {
                return (number & (int)0xFFFF0000) | (newValue & 0x0000FFFF);
            }
        }

        public static int WriterCount(this int number)
        {
            unchecked
            {
                return number >> 16;
            }
        }

        public static int SetWriterCount(this int number, int newValue)
        { return (number & 0x0000FFFF) + (newValue << 16); }
    }
}
