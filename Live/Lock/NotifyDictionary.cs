using System;
using System.Collections.Generic;

namespace Vertigo.Live
{
    public class NotifyDictionary<TKey, TValue> : NotifyObject<Dictionary<TKey,TValue>>
    {
        public void Set(TKey key, TValue value, bool notify = true)
        {
            Apply(dictionary =>
                      {
                          dictionary.Add(key, value);
                          return true;
                      }, notify);
        }

        public void Remove(TKey key, bool notify = true)
        {
            Apply(dictionary => dictionary.Remove(key), notify);
        }
    }
}
