using System.Collections.Generic;

namespace Vertigo.Live
{
    public class NotifyList<T> : NotifyObject<List<T>>
    {
        public void Add(T item, bool notify = true)
        {
            Apply(list =>
            {
                list.Add(item);
                return true;
            },
                notify);
        }
    }
}
