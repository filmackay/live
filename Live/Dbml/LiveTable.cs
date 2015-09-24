using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Vertigo.Live
{
    public interface ILiveTable
    {
        LiveDataContext DataContext { get; }
        IDatabaseAction[] GetChanges();
        string PrepareMergeSql { get; }
        void UploadMerge(DatabaseConnection connection, IEnumerable<IDatabaseAction> actions);
        string CompleteMergeSql { get; }
    }

    public class LiveTable<T> : LiveSet<T, LiveTableInner<T>, LiveTable<T>>, ILiveTable
        where T : class, ILiveEntity<T>, new()
    {
        private readonly LiveDataContext _dataContext;
        internal readonly Dictionary<T, LivePropertiesSubscription<T>> _observers = new Dictionary<T, LivePropertiesSubscription<T>>();
        private readonly NotifyDictionary<LivePropertiesSubscription<T>, DatabaseActionType> _changes;
        private static readonly string _tableName;
        private static readonly Tuple<PropertyInfo, ColumnAttribute>[] _columns;
        private static Tuple<PropertyInfo, ColumnAttribute>[] _writeColumns;

        static LiveTable()
        {
            _tableName = typeof (T).CustomAttribute<TableAttribute>().Name;
            _columns = typeof(T)
                .GetProperties()
                .Select(p => new Tuple<PropertyInfo, ColumnAttribute>(p, p.CustomAttribute<ColumnAttribute>()))
                .Where(p => p.Item2 != null)
                .ToArray();
            _writeColumns = _columns
                .Where(p => !p.Item2.DbType.Contains("IDENTITY") && !p.Item2.IsDbGenerated && !p.Item2.IsVersion)
                .ToArray();
        }
        
        public LiveTable(LiveDataContext dataContext)
        {
            _dataContext = dataContext;
            _changes = new NotifyDictionary<LivePropertiesSubscription<T>, DatabaseActionType> { OnNotify = _dataContext.NotifyTableChange };

            PrepareMergeSql = string.Format("SELECT CAST('' AS NVARCHAR(10)) AS __Action__, {1} INTO #action FROM {0} WHERE 1=0;\n",
                _tableName,
                string.Join(",", _writeColumns.Select(c => string.Format("[{0}]", c.Item2.Name)).ToArray()));
            CompleteMergeSql = string.Format("MERGE {0} AS t\n" +
                "USING #action AS s\n" +
                "	ON ({3} AND s.__Action__<>'DELETE')\n" +
                "WHEN NOT MATCHED BY TARGET\n" +
                "	THEN INSERT ({1}) VALUES ({2})\n" +
                "WHEN MATCHED\n" +
                "    THEN UPDATE SET t.[EventID]=s.[EventID],t.[VenueID]=s.[VenueID],t.[StartTime]=s.[StartTime],t.[EventType]=s.[EventType],t.[RaceNumber]=s.[RaceNumber],t.[RaceName]=s.[RaceName],t.[Suspended]=s.[Suspended];\n" +
                "DELETE t FROM {0} AS t JOIN #action AS s ON {3} WHERE __Action__='DELETE';",
                _tableName,
                string.Join(",", _writeColumns.Select(c => string.Format("[{0}]", c.Item2.Name)).ToArray()),
                string.Join(",", _writeColumns.Select(c => string.Format("s.[{0}]", c.Item2.Name)).ToArray()),
                string.Join(" AND ", _columns.Where(c => c.Item2.IsPrimaryKey).Select(c => string.Format("t.[{0}]=s.[{0}]", c.Item2.Name)).ToArray()));
        }

        public LiveDataContext DataContext { get { return _dataContext; } }

        internal void ApplyAction(LivePropertiesSubscription<T> observer, DatabaseActionType action)
        {
            if (action == DatabaseActionType.Insert)
                _observers.Add(observer.Observable, observer);
            _changes.Notify(changes =>
            {
                var oldAction = DatabaseActionType.None;
                var found = changes.TryGetValue(observer, out oldAction);
                var newAction = oldAction.Merge(DatabaseActionType.Insert);
                if (newAction != DatabaseActionType.None)
                    changes[observer] = newAction;
                else if (found)
                    changes.Remove(observer);
            });
            if (action == DatabaseActionType.Delete)
                _observers.Remove(observer.Observable);
        }

        public DatabaseAction<T>[] GetChanges()
        {
            var changes = _changes.Get();
            return changes == null ? new DatabaseAction<T>[] {} : changes.Select(change => new DatabaseAction<T>(change.Key.Observable, change.Value)).ToArray();
        }

        IDatabaseAction[] ILiveTable.GetChanges()
        {
            return GetChanges();
        }

        public void Load(string whereFilter = null)
        {
            // read table
            _inner.Clear();
            using (var db = _dataContext.DatabaseConnection())
            {
                var sql = string.Format("SELECT * FROM {0}", _tableName);
                if (whereFilter != null)
                    sql += " WHERE " + whereFilter;
                var cmd = new SqlCommand(sql, db.SqlConnection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.NextResult())
                    {
                        var entity = new T();
                        foreach (var column in _columns)
                        {
                            var @value = reader[column.Item2.Name];
                            var liveProperty = column.Item1.GetValue(entity, null) as ILiveValuePublisher;
                            liveProperty.PublishValue = @value;
                        }
                        _inner._Add(entity, false);
                    }
                }
            }
        }

        public string PrepareMergeSql { get; private set; }

        public void UploadMerge(DatabaseConnection connection, IEnumerable<DatabaseAction<T>> actions)
        {
            // insert into temp table
            var bulk = new SqlBulkCopy(connection.SqlConnection, SqlBulkCopyOptions.TableLock, connection.SqlTransaction) { BulkCopyTimeout = 0 };
            var reader = new DatabaseActionReader<T>(actions);
            foreach (var column in reader.ColumnMappingList)
                bulk.ColumnMappings.Add(column.Key, column.Key);
            bulk.DestinationTableName = "#action";
            bulk.WriteToServer(reader);
        }

        public void UploadMerge(DatabaseConnection connection, IEnumerable<IDatabaseAction> actions)
        {
            UploadMerge(connection, actions.OfType<DatabaseAction<T>>());
        }

        public string CompleteMergeSql { get; private set; }
    }

    public class LiveTableInner<T> : LiveSetInner<T, LiveTableInner<T>, LiveTable<T>>
        where T : class, ILiveEntity<T>, new()
    {
        internal bool _Add(T item, bool insert)
        {
            Debug.Assert(item.Table == null);
            var ret = base._Add(item);

            // if add was successful, point entity to table
            if (ret)
            {
                item.InternalAttach(_parent);
                item.Observe(insert ? (Action<LivePropertiesSubscription<T>>)(observer => _parent.ApplyAction(observer, DatabaseActionType.Insert)) : observer => { },
                             observer => _parent.ApplyAction(observer, DatabaseActionType.Update));
            }
            return ret;
        }

        protected override bool _Add(T item)
        {
            return _Add(item, true);
        }

        public bool Populate(T item)
        {
            return _Add(item, false);
        }

        internal bool Remove(T item, bool delete)
        {
            Debug.Assert(item.Table == _parent);
            var ret = base.Remove(item);
            // if remove was successful - un-point entity to table
            if (ret)
            {
                // find observer, and unsubscribe
                var observer = _parent._observers[item];
                if (delete)
                    _parent.ApplyAction(observer, DatabaseActionType.Delete);
                item.InternalAttach(null);
                observer.Dispose();
            }
            return ret;
        }

        public override bool Remove(T item)
        {
            return Remove(item, true);
        }
    }
}