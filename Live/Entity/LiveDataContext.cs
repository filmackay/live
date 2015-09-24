using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vertigo.Live
{
    public abstract partial class LiveDataContext<TDataContext> : Logger
        where TDataContext : LiveDataContext<TDataContext>
    {
		private readonly string _connectionString;
        private readonly NotifyLock _notify;

        protected LiveDataContext(string connectionString)
        {
            _connectionString = connectionString;
            _notify = new NotifyLock
            {
                OnNotify =
                    () => Publish.OnConsume(
                        () =>
                        {
                            var start = HiResTimer.Now();
                            Commit();
                            var finish = HiResTimer.Now();
                            var ms = HiResTimer.ToTimeSpan(finish - start).TotalMilliseconds;
                            if (ms > 3)
                                Log.Info("Pre-database commit: {0}ms", ms);
                        })
            };
        }

        public LiveTable<T, TDataContext> GetTable<T>()
            where T : class, ILiveEntity<T, TDataContext>, new()
        {
            var propertyInfo = GetType()
                .GetProperties()
                .FirstOrDefault(p => p.PropertyType == typeof(LiveTable<T, TDataContext>));
            return propertyInfo == null ? null : propertyInfo.GetValue(this, null) as LiveTable<T, TDataContext>;
        }

        public DatabaseConnection DatabaseConnection(bool beginTransaction = false)
        {
            var ret = new DatabaseConnection(_connectionString);
            if (beginTransaction)
                ret.BeginTransaction();
            return ret;
        }

        public abstract void JoinTables();

        public abstract ILiveTable<TDataContext>[] Tables { get; }

        public void Add(ILiveEntity<TDataContext> entity, bool insertToDatabase)
        {
            if (entity.DataContext == this)
            {
                // we are already attached to entity
                return;
            }
            if (entity.DataContext != null)
            {
                // attached to another entity
                throw new InvalidOperationException("Entity already attached to another DataContext");
            }

            // link to table
            entity.DataContext = this as TDataContext;
            entity.Table.Add(entity, insertToDatabase);
        }

        public void Commit()
        {
            var notifyProcess = _notify.Process();

            // get all database changes with consistency
            var tables = Tables
                .Select(table => Tuple.Create(table.GetDatabaseActions(), table))
                .Where(t => t.Item1 != null && t.Item1.Count != 0)
                .ToArray();
            if (tables.Length == 0)
            {
                notifyProcess.Dispose();
                return;
            }

            // write to database asynchronously
            Task.Run(() =>
                {
                    using (notifyProcess)
                    using (var connection = DatabaseConnection())
                    {
                        // prepare merges
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = string.Join(";\n", tables.Select(t => t.Item2.PrepareMergeSql));
                            cmd.ExecuteNonQuery();
                        }

                        // upload data
                        tables.ForEach(table => table.Item2.UploadMerge(connection, table.Item1));

                        // complete merges
                        var sql = string.Join(";\n", tables.Select(table => table.Item2.CompleteMergeSql));
                        try
                        {
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                    }
                });
        }

        public void NotifyTableChange()
        {
            _notify.Notify();
        }
    }
}
