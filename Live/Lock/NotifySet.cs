using System.Collections.Generic;

namespace Vertigo.Live
{
    public class NotifySet<T> : NotifyObject<HashSet<T>>
    {
        public void Add(T item, bool notify = true)
        {
            var added = false;
            Apply(set => added = set.Add(item), false);
            if (notify && added)
                Notify();
        }
    }
}
