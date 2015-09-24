using System.Collections.Generic;
using System.Diagnostics;

namespace Vertigo.Live
{
    public interface ILiveEntity<TDataContext>
        where TDataContext : LiveDataContext<TDataContext>
    {
        object[] GetWriteValues { get; }
        TDataContext DataContext { get; set; }
        IEnumerable<ILiveEntity<TDataContext>> Children { get; }
        IEnumerable<ILiveEntityParentRef> Parents { get; }
        ILiveTable<TDataContext> Table { get; }
    }

    public interface ILiveEntity<TEntity, TDataContext> : ILiveEntity<TDataContext>
        where TEntity : class, ILiveEntity<TEntity, TDataContext>, new()
        where TDataContext : LiveDataContext<TDataContext>
    {
        new LiveTable<TEntity, TDataContext> Table { get; }
    }

    public interface ILiveEntityParentRef
    {
        void Detach();
    }

    public static partial class Extensions
    {
        public static void Remove<TDataContext>(this ILiveEntity<TDataContext> entity, bool deleteFromDatabase)
            where TDataContext : LiveDataContext<TDataContext>
        {
            entity.Table.Remove(entity, deleteFromDatabase);
        }
    }
}