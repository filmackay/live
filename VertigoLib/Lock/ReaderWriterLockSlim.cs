using System;
using System.Reactive.Disposables;
using System.Threading;

namespace Vertigo
{
    public static class ReaderWriterLockSlimExtensions
    {
        public static IDisposable ReadLock(this ReaderWriterLockSlim @lock, bool block = true)
        {
            if (block)
                @lock.EnterReadLock();
            else if (!@lock.TryEnterReadLock(0))
                return null;
            return Disposable.Create(@lock.ExitReadLock);
        }

        public static IDisposable WriteLock(this ReaderWriterLockSlim @lock, bool block = true)
        {
            if (block)
                @lock.EnterWriteLock();
            else if (!@lock.TryEnterWriteLock(0))
                return null;
            return Disposable.Create(@lock.ExitWriteLock);
        }

        public static IDisposable UpgradableReadLock(this ReaderWriterLockSlim @lock, bool block = true)
        {
            if (block)
                @lock.EnterUpgradeableReadLock();
            else if (!@lock.TryEnterUpgradeableReadLock(0))
                return null;
            return Disposable.Create(@lock.ExitUpgradeableReadLock);
        }
    }
}
