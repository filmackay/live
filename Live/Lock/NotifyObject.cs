using System;

namespace Vertigo.Live
{
    public class NotifyObject<T> : NotifyLock
        where T : class, new()
    {
        public void Apply(Func<T, bool> process, bool notify = true)
        {
            bool impacted;
            using (this.Lock())
            {
                // create object
                if (_object == null)
                    _object = new T();

                // provide access
                impacted = process(_object);
            }

            if (impacted && notify)
                Notify();
        }

        public void ProcessGet(Action<Func<T>> process)
        {
            Process((notified, commit) =>
                // get value
                process(() =>
                {
                    // commit now we are getting the value
                    commit();

                    // get object
                    return GetObject();
                }));
        }

        private T GetObject()
        {
            // get object
            using (this.Lock())
            {
                var oldObject = _object;
                _object = null;
                return oldObject;
            }
        }

        public T Get()
        {
            var @object = default(T);
            ProcessGet(getValue => @object = getValue());
            return @object;
        }

        private T _object = new T();
    }
}
