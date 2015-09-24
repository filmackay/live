//#define DEBUG_ReaderWriterSpinLock

using System;
using System.Diagnostics;
using System.Threading;
using Vertigo.Live;


namespace Vertigo
{
    // We use plenty of interlocked operations on volatile fields below.  Safe.
    #pragma warning disable 0420

    public enum LockStatus
    {
        None,
        Reader,
        Writer
    }

    public delegate ILock ReaderWriterLocker(bool block = true);

    public interface ILock : IDisposable
    {
        bool Release(); // true means lock is now not held by anyone
        void DowngradeToReader();
        bool UpgradeToWriter(bool block = true);
    }

    public interface IReaderWriterLockable
    {
        ILock WriteLock(bool block = true);
        ILock ReadLock(bool block = true);
    }

    /// <summary>
    /// A very lightweight reader/writer lock.  It uses a single word of memory, and
    /// only spins when contention arises (no events are necessary).
    /// This lock has no thread affinity.
    /// Based on: http://www.bluebytesoftware.com/blog/2009/01/30/ASinglewordReaderwriterSpinLock.aspx
    /// Changes:
    /// - class not struct, since they dont work with lambdas
    /// - removed support waiting writers, thereby giving priority to readers
    /// - added upgrade/downgrade
    /// - added IDisposable wrappers (Lock class)
    /// </summary>
    public class ReaderWriterSpinLock : IReaderWriterLockable
    {
        private volatile int m_state;
        private const int MASK_WRITER_BIT = unchecked((int)0x80000000);
        private const int MASK_READER_BITS = unchecked(~MASK_WRITER_BIT);
#if DEBUG
        private const int _spinThreshold = 100;
        private volatile int m_lastWriteThreadID;
#endif
#if DEBUG_ReaderWriterSpinLock
        public int ID;
#else
        public int ID { set { } }
#endif

        public ReaderWriterSpinLock()
        {
            ID = GetHashCode();
        }

        public bool EnterWriteLock(bool block)
        {
            var start = HiResTimer.Now();
#if DEBUG
            if (m_lastWriteThreadID == Thread.CurrentThread.ManagedThreadId)
                throw new SynchronizationLockException("Write lock already owned by you");
#endif

            var sw = new SpinWait();
            do
            {
                // If there are no readers or writers, grab the write lock.
                var state = m_state;
                if (state == 0 &&
                    Interlocked.CompareExchange(ref m_state, MASK_WRITER_BIT, state) == state)
                {
#if DEBUG
                    m_lastWriteThreadID = Thread.CurrentThread.ManagedThreadId;
                    if (sw.Count > _spinThreshold)
                        Debug.Print("EnterWriteLock: {0} spins ({1:F1}ms)", sw.Count, HiResTimer.ToTimeSpan(HiResTimer.Now() - start).TotalMilliseconds);
#endif
#if DEBUG_ReaderWriterSpinLock
                    if (ID != 0)
                        Debug.Print("{0} Enter Write on #{1} ({2})", Thread.CurrentThread.ManagedThreadId, ID, m_lastWriteThreadID);
#endif

                    return true;
                }

                sw.SpinOnce();
            }
            while (block);
            return false;
        }

        public void ExitWriteLock()
        {
            if (m_state != MASK_WRITER_BIT)
                throw new SynchronizationLockException("Write lock is not held");

#if DEBUG
            m_lastWriteThreadID = 0;
#endif
#if DEBUG_ReaderWriterSpinLock
            if (ID != 0)
                Debug.Print("{0} Exit Write on #{1} ({2})", Thread.CurrentThread.ManagedThreadId, ID, m_lastWriteThreadID);
#endif

            // Exiting the write lock is simple: just set the state to 0.  We
            // try to keep the writer waiting bit to prevent readers from getting
            // in -- but don't want to resort to a CAS, so we may lose one.
            m_state = 0;
        }

        public void DowngradeWriteLock()
        {
            var sw = new SpinWait();
            do
            {
                var state = m_state;
                if (m_state != MASK_WRITER_BIT)
                    throw new SynchronizationLockException("Write lock is not held");
                if (Interlocked.CompareExchange(ref m_state, 1, state) == state)
                {
#if DEBUG
                    m_lastWriteThreadID = 0;
                    if (sw.Count > _spinThreshold)
                        Debug.Print("DowngradeWriteLock: {0} spins", sw.Count);
#endif
#if DEBUG_ReaderWriterSpinLock
                    Debug.Print("{0} Downgrade on #{1} ({2})", Thread.CurrentThread.ManagedThreadId, ID, m_lastWriteThreadID);
#endif
                    return;                    
                }

                sw.SpinOnce();
            }
            while (true);
        }

        public bool UpgradeReadLock(bool block)
        {
            var sw = new SpinWait();
            do
            {
                var state = m_state;
                if ((state & MASK_READER_BITS) == 0)
                    throw new InvalidOperationException("No read lock is held");
                if (state == 1 &&
                    Interlocked.CompareExchange(ref m_state, MASK_WRITER_BIT, state) == state)
                {
#if DEBUG_ReaderWriterSpinLock
                    Debug.Print("{0} Upgrade on #{1} ({2})", Thread.CurrentThread.ManagedThreadId, ID, m_lastWriteThreadID);
#endif
#if DEBUG
                    m_lastWriteThreadID = Thread.CurrentThread.ManagedThreadId;
                    if (sw.Count > _spinThreshold)
                        Debug.Print("UpgradeReadLock: {0} spins", sw.Count);
#endif
                    return true;
                }

                sw.SpinOnce();
            }
            while (block);
            return false;
        }

        public ILock WriteLock(bool block = true)
        {
            return EnterWriteLock(block)
                ? new Lock(this, LockStatus.Writer)
                : null;
        }

        public bool EnterReadLock(bool block)
        {
            var sw = new SpinWait();
            do
            {
                var state = m_state;
                if ((state & MASK_WRITER_BIT) == 0 &&
                    Interlocked.CompareExchange(ref m_state, state + 1, state) == state)
                {
#if DEBUG_ReaderWriterSpinLock
                    Debug.Print("{0} Enter Read #{1} ({2}) [{3}]", Thread.CurrentThread.ManagedThreadId, ID, m_lastWriteThreadID, state + 1);
#endif
#if DEBUG
                    if (sw.Count > _spinThreshold)
                        Debug.Print("EnterReadLock: {0} spins", sw.Count);
#endif
                    return true;
                }

                sw.SpinOnce();
            }
            while (block);
            return false;
        }

        public bool ExitReadLock()
        {
            var sw = new SpinWait();
            do
            {
                // Validate we hold a read lock.
                var state = m_state;
                if ((state & MASK_READER_BITS) == 0)
                    throw new SynchronizationLockException("Cannot exit read lock when there are no readers");

                // Try to exit the read lock, preserving the writer waiting bit (if any).
                if (Interlocked.CompareExchange(ref m_state, state - 1, state) == state)
                {
#if DEBUG_ReaderWriterSpinLock
                    Debug.Print("{0} Exit Read #{1} ({2}) [{3}]", Thread.CurrentThread.ManagedThreadId, ID, m_lastWriteThreadID, state - 1);
#endif
#if DEBUG
                    if (sw.Count > _spinThreshold)
                        Debug.Print("ExitReadLock: {0} spins", sw.Count);
#endif
                    return state == 1;
                }

                sw.SpinOnce();
            }
            while (true);
        }

        public ILock ReadLock(bool block = true)
        {
            return EnterReadLock(block)
                ? new Lock(this, LockStatus.Reader)
                : null;
        }

        public bool IsWriteLockHeld
        {
            get { return (m_state & MASK_WRITER_BIT) == MASK_WRITER_BIT; }
        }

        public int CurrentReadCount
        {
            get { return m_state & MASK_READER_BITS; }
        }

        private class Lock : ILock
        {
            private readonly ReaderWriterSpinLock _parent;
            private LockStatus _status;

            public Lock(ReaderWriterSpinLock parent, LockStatus status)
            {
                _parent = parent;
                _status = status;
            }

            public void Dispose()
            {
                Release();
            }

            public bool Release()
            {
                var ret = false;
                switch (_status)
                {
                    case LockStatus.Reader:
                        ret = _parent.ExitReadLock();
                        break;
                    case LockStatus.Writer:
                        _parent.ExitWriteLock();
                        ret = true;
                        break;
                }
                _status = LockStatus.None;
                return ret;
            }

            public void DowngradeToReader()
            {
                if (_status == LockStatus.Writer)
                {
                    _parent.DowngradeWriteLock();
                    _status = LockStatus.Reader;
                }
                else
                    throw new SynchronizationLockException("Write lock not held");
            }

            public bool UpgradeToWriter(bool block = true)
            {
                if (_status != LockStatus.Reader)
                    throw new SynchronizationLockException("Read lock not held");
                if (_parent.UpgradeReadLock(block))
                {
                    _status = LockStatus.Writer;
                    return true;
                }
                return false;
            }
        }
    }
}
