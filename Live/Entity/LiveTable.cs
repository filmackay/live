using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    public interface ILiveTable<TDataContext>
        where TDataContext : LiveDataContext<TDataContext>
    {
        Type EntityType { get; }
        TDataContext DataContext { get; }
        DatabaseActions GetDatabaseActions();
        string PrepareMergeSql { get; }
        void UploadMerge(DatabaseConnection connection, DatabaseActions actions);
        string CompleteMergeSql { get; }
        void Add(ILiveEntity<TDataContext> entity, bool insertToDatabase);
        void Remove(ILiveEntity<TDataContext> entity, bool deleteFromDatabase);
    }

    public interface ILiveTable<in TEntity, TDataContext> : ILiveTable<TDataContext>
        where TEntity : class, ILiveEntity<TEntity, TDataContext>, new()
        where TDataContext : LiveDataContext<TDataContext>
    {
        void Add(TEntity entity, bool insertToDatabase);
        void Remove(TEntity entity, bool deleteFromDatabase);
    }

    public class LiveTable<TEntity, TDataContext> : LiveSet<TEntity, LiveTableInner<TEntity, TDataContext>, LiveTable<TEntity, TDataContext>>, ILiveTable<TEntity, TDataContext>
        where TEntity : class, ILiveEntity<TEntity, TDataContext>, new()
        where TDataContext : LiveDataContext<TDataContext>
    {
        private readonly TDataContext _dataContext;
        private readonly NotifyDatabaseActions _committedDatabaseActions;
        private readonly NotifyDatabaseActions _uncommittedDatabaseActions;
        private readonly Dictionary<TEntity, LivePropertiesSubscription> _subscriptions = new Dictionary<TEntity, LivePropertiesSubscription>();
        private static readonly string _tableName;
        private static readonly string _tempTableName;
        private static readonly Tuple<PropertyInfo, ColumnAttribute>[] _columns;
        private static readonly string[] _writeColumnNames;

        static LiveTable()
        {
            _tableName = typeof(TEntity).CustomAttribute<TableAttribute>().Name.Replace("dbo.", "");
            _tempTableName = "#" + _tableName;
            _columns = typeof(TEntity)
                .GetProperties()
                .Select(p => new Tuple<PropertyInfo, ColumnAttribute>(p, p.CustomAttribute<ColumnAttribute>()))
                .Where(p => p.Item2 != null)
                .ToArray();
            _writeColumnNames = _columns
                .Where(p => !p.Item2.DbType.ToUpper().Contains("IDENTITY") && !p.Item2.IsDbGenerated && !p.Item2.IsVersion)
                .Select(c => c.Item2.Name)
                .ToArray();
        }

        public LiveTable(TDataContext dataContext)
            : base(new HashSet<TEntity>(), new HashSet<TEntity>(), new TEntity[] {})
        {
            _dataContext = dataContext;
            _committedDatabaseActions =
                new NotifyDatabaseActions
                {
                    OnNotify = () => Publish.OnConsume(_dataContext.NotifyTableChange)
                };
            _uncommittedDatabaseActions =
                new NotifyDatabaseActions
                {
                    OnNotify = () => Publish.OnPublish(Commit)
                };

            PrepareMergeSql = string.Format(
                //"IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{1}]') AND type in (N'U'))\n" +
                //"BEGIN\nDELETE FROM [{1}]\nEND\n" +
                //"ELSE\nBEGIN\n" +
                "SELECT CAST('' AS NVARCHAR(10)) AS __Action__, {2} INTO [{1}] FROM [{0}] WHERE 1=0;\n"
                // + "END\n"
                ,
                _tableName,
                _tempTableName,
                string.Join(",", _writeColumnNames.Select(c => string.Format("[{0}]", c)).ToArray()));
            var updateColumns = string.Join(",", _columns.Where(c => !c.Item2.IsPrimaryKey).Select(c => string.Format("t.[{0}]=s.[{0}]", c.Item2.Name)).ToArray());
            CompleteMergeSql = string.Format(
                "MERGE INTO [{0}] AS t\n" +
                "USING (SELECT * FROM [{1}] WHERE __Action__<>'Delete') AS s\n" +
                "	ON {4}\n" +
                "WHEN NOT MATCHED BY TARGET THEN INSERT ({2}) VALUES ({3})\n" +
                "{5};\n" +
                "DELETE t FROM [{0}] AS t JOIN [{1}] AS s ON {4} WHERE __Action__='Delete';\n",
                    _tableName,
                    _tempTableName,
                    string.Join(",", _writeColumnNames.Select(c => string.Format("[{0}]", c)).ToArray()),
                    string.Join(",", _writeColumnNames.Select(c => string.Format("s.[{0}]", c)).ToArray()),
                    string.Join(" AND ", _columns.Where(c => c.Item2.IsPrimaryKey).Select(c => string.Format("t.[{0}]=s.[{0}]", c.Item2.Name)).ToArray()),
                    updateColumns.Length == 0 ? "" : "WHEN MATCHED THEN UPDATE SET " + updateColumns
                    );
        }

        public TDataContext DataContext { get { return _dataContext; } }

        public Type EntityType
        {
            get { return typeof (TEntity); }
        }

        protected override void Commit()
        {
            base.Commit();
            _committedDatabaseActions.Apply(_uncommittedDatabaseActions);
        }

        public DatabaseActions GetDatabaseActions()
        {
            var actionDictionary = _committedDatabaseActions.Get();
            if (actionDictionary == null)
                return null;

            return new DatabaseActions(_writeColumnNames, actionDictionary.Select(change => new Tuple<DatabaseActionType, object[]>(change.Value, change.Key.GetWriteValues)));
        }

        public Task Load()
        {
            return Load(null, null);
        }

        public Task Load(string sqlFilter)
        {
            return Load(sqlFilter, null);
        }

        public Task Load(Func<TEntity, bool> entityfilter)
        {
            return Load(null, entityfilter);
        }

        public async Task Load(string sqlFilter, Func<TEntity, bool> entityfilter)
        {
            // read table
            _publishInner.Clear();
            using (var db = _dataContext.DatabaseConnection())
            {
                var sql = string.Format("SELECT * FROM {0}", _tableName);
                if (sqlFilter != null)
                    sql += " WHERE " + sqlFilter;
                var cmd = new SqlCommand(sql, db.SqlConnection);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var entity = new TEntity();
                        foreach (var column in _columns)
                        {
                            var @value = reader[column.Item2.Name];

                            // set value
                            var liveProperty = column.Item1.GetValue(entity, null);
                            var liveBase =
                                liveProperty
                                    .GetType()
                                    .GetGenericBase(typeof(Live<>));
                            liveBase
                                .GetMethod("InitUntyped")
                                .Invoke(liveProperty, new[] {@value, 0L});
                        }
                        if (entityfilter == null || entityfilter(entity))
                            Add(entity, false);
                    }
                }
            }
        }

        public void JoinTrim<TParent, TKey>(IEnumerable<TParent> parents, Func<TEntity, TKey> childKeySelector, Func<TParent, TKey> parentKeySelector, Action<TEntity, TParent> doJoin, bool remove)
        {
            var ps = parents.ToDictionary(parentKeySelector);
            this.ToArray()
                .ForEach(child =>
                    {
                        TParent parent;
                        if (ps.TryGetValue(childKeySelector(child), out parent))
                            // child matches parent
                            doJoin(child, parent);
                        else if (remove && child.Table != null)
                            // remove child
                            Remove(child, false);
                    });
        }

        public string PrepareMergeSql { get; private set; }

        public void UploadMerge(DatabaseConnection connection, DatabaseActions actions)
        {
            // insert into temp table
            var bulk = new SqlBulkCopy(connection.SqlConnection, SqlBulkCopyOptions.TableLock, connection.SqlTransaction)
                {
                    BulkCopyTimeout = 0,
                    DestinationTableName = _tempTableName
                };
            bulk.WriteToServer(actions);
        }

        public string CompleteMergeSql { get; private set; }

        public void AddRange(IEnumerable<TEntity> entities, bool insertToDatabase)
        {
            foreach (var entity in entities)
                Add(entity, insertToDatabase);
        }

        public void Add(TEntity entity, bool insertToDatabase)
        {
            if (entity.DataContext == null)
                entity.DataContext = _dataContext;
            else if (entity.DataContext != _dataContext)
                throw new InvalidOperationException("Entity not attached to this data context");

            // set datacontext
            PublishInner.Add(entity);

            // subscribe to all changes
            var subscription = entity.SubscribeToColumns();
            using (_subscriptions.Lock())
                _subscriptions[entity] = subscription;
            subscription.Connect(() =>
            {
                // this only reports changes - not start/finish
                subscription.ClearChanges();
                _uncommittedDatabaseActions.Apply(entity, DatabaseActionType.Update);
            });

            // insert to database
            if (insertToDatabase)
                _uncommittedDatabaseActions.Apply(entity, DatabaseActionType.Insert);

            // process children
            foreach (var child in entity.Children)
                _dataContext.Add(child, insertToDatabase);
        }

        public void Add(ILiveEntity<TDataContext> entity, bool insertToDatabase)
        {
            var item = entity as TEntity;
            if (item == null)
            {
                // attached to another entity
                throw new InvalidOperationException("Item is not the correct type");
            }
            Add(item, insertToDatabase);
        }

        public void Remove(TEntity entity, bool deleteFromDatabase)
        {
            Debug.Assert(entity.Table == this);
            if (deleteFromDatabase)
                _uncommittedDatabaseActions.Apply(entity, DatabaseActionType.Delete);

            // remove from datacontext
            entity.DataContext = null;
            PublishInner.Remove(entity);

            using (_subscriptions.Lock())
            {
                LivePropertiesSubscription subscription;
                if (!_subscriptions.TryGetValue(entity, out subscription))
                {
                    Debug.Assert(false);
                    return;
                }
                subscription.Dispose();
                _subscriptions.Remove(entity);
            }

            // process children
            foreach (var child in entity.Children)
                if (child.Table != null)
                    child.Table.Remove(child, deleteFromDatabase);
            foreach (var parent in entity.Parents)
                parent.Detach();
        }

        public void Remove(ILiveEntity<TDataContext> entity, bool deleteFromDatabase)
        {
            var item = entity as TEntity;
            if (item == null)
                throw new InvalidOperationException("Item is not the correct type");
            Remove(item, deleteFromDatabase);
        }

        public void Remove(Func<TEntity,bool> filter, bool deleteFromDatabase = false)
        {
            this.ToArray()
                .Where(filter)
                .ForEach(s => Remove(s, deleteFromDatabase));
        }

        public class NotifyDatabaseActions : NotifyObject<Dictionary<TEntity, DatabaseActionType>>
        {
            private static bool _Apply(Dictionary<TEntity, DatabaseActionType> actions, TEntity item, DatabaseActionType action)
            {
                // find existing actions on this item
                DatabaseActionType oldAction;
                var found = actions.TryGetValue(item, out oldAction);

                // work out new action
                var newAction = oldAction.Add(action);
                if (oldAction == newAction)
                    return false;

                // set new action
                if (newAction != DatabaseActionType.Unchanged)
                    actions[item] = newAction;
                else if (found)
                    actions.Remove(item);
                return true;
            }

            public void Apply(TEntity item, DatabaseActionType action)
            {
                Apply(actions => _Apply(actions, item, action));
            }

            public void Apply(NotifyDatabaseActions newNotifyActions)
            {
                Apply(actions =>
                    {
                        var newActions = newNotifyActions.Get();
                        if (newActions == null || newActions.Count == 0)
                            return false;

                        return newActions
                            .Select(kv => _Apply(actions, kv.Key, kv.Value))
                            .ToArray()
                            .Any();
                    });
            }
        }
    }

    public class LiveTableInner<T, TDataContext> : LiveSetInner<T, LiveTableInner<T, TDataContext>, LiveTable<T, TDataContext>>
        where T : class, ILiveEntity<T, TDataContext>, new()
        where TDataContext : LiveDataContext<TDataContext>
    {
    }
}