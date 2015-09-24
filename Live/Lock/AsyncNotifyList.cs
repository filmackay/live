using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    public class AsyncNotifyList<T> : AsyncNotifyObject<List<T>>
    {
        public Task Add(T item)
        {
            return Apply(list => list.Add(item));
        }
    }
}
