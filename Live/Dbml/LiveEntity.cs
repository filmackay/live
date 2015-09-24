using System;
using System.Collections.Generic;
using System.Data.Linq;

namespace Vertigo.Live
{
    public interface ILiveEntity<TEntity>
        where TEntity : class, ILiveEntity<TEntity>, new()
    {
        LiveTable<TEntity> Table { get; set;  }
        LiveDataContext DataContext { get; set; }
        void InternalAttach(LiveTable<TEntity> table);
    }

    public static partial class Extensions
    {
        public static void Observe<TEntity>(this TEntity entity, Action<LivePropertiesSubscription<TEntity>> setObserver, Action<LivePropertiesSubscription<TEntity>> notifyChange)
            where TEntity : class, ILiveEntity<TEntity>, new()
        {
            new LivePropertiesSubscription<TEntity>(entity, setObserver, notifyChange);
        }
    }
}