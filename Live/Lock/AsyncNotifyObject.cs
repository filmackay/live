using System;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    public abstract class AsyncNotifyObject<T> : AsyncNotifyLock
        where T : class, new()
    {
        public Task Apply(Action<T> process)
        {
            using (this.Lock())
            {
                // provide access
                process(_object);
            }

            return Notify();
        }

        //public async Task<T> Get()
        //{
        //    var @object = default(T);
        //    await Process(async () =>
        //            {
        //                using (this.Lock())
        //                {
        //                    @object = _object;
        //                    _object = new T();
        //                }
        //            });
        //    return @object;
        //}

        private T _object = new T();
    }
}
