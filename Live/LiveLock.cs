using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Vertigo.Live
{
    public interface ILiveLockable : IDisposable
    {
        IDisposable ReadLock(bool block);
        IDisposable WriteLock(bool block);
    }

    public class LiveLockable : ILiveLockable
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public IDisposable ReadLock(bool block)
        {
            if (block)
                _lock.EnterReadLock();
            else
            {
                if (!_lock.TryEnterReadLock(0))
                    return null;
            }
            return new AnonymousDisposable(() => _lock.ExitReadLock());
        }

        public IDisposable WriteLock(bool block)
        {
            if (block)
                _lock.EnterWriteLock();
            else
            {
                if (!_lock.TryEnterWriteLock(0))
                    return null;
            }
            return new AnonymousDisposable(() => _lock.ExitWriteLock());
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }

    public static partial class Extensions
    {
        public static void Dispose(this IEnumerable<IDisposable> locks)
        {
            foreach (var @lock in locks)
                @lock.Dispose();
        }

        public static IDisposable ReadLock<T>(this IEnumerable<T> items)
            where T : ILiveLockable
        {
            return items.OfType<ILiveLockable>().ToArray().ReadLock();
        }

        public static IDisposable ReadLock(this ILiveLockable[] items)
        {
            // obtains a read lock on all items
            if (items.Length == 0)
                return AnonymousDisposable.Null;

            using (new TimeMonitor("ReadLock"))
            {
                var firstIndex = 0;      // first lock to try obtaining
                while (true)
                {
                    // get first lock
                    var firstLock = items[firstIndex].ReadLock(true);

                    // get remaining locks, until we are unable to
                    var locks = items
                        .Select((item, index) => index == firstIndex ? firstLock : item.ReadLock(false))
                        .TakeWhile(itemLock => itemLock != null)
                        .ToArray();
                    if (locks.Length == items.Length)
                    {
                        // we got them all
                        return new AnonymousDisposable(locks.Dispose);
                    }

                    // couldn't get them all - release to avoid deadlock
                    locks.Dispose();

                    // next time, start with the one that we could not get a lock on this time
                    firstIndex = locks.Length;
                }
            }
        }
    }
}