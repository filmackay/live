using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;

namespace Vertigo.Live
{
    public abstract class LiveDataContext
    {
		private readonly string _connectionString;
        private readonly ThrottleTimeline _timeline = new ThrottleTimeline(TimeSpan.FromSeconds(10), null);

        protected LiveDataContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public LiveTable<T> GetTable<T>()
            where T : class, ILiveEntity<T>, new()
        {
            var propertyInfo = GetType()
                .GetProperties()
                .FirstOrDefault(p => p.PropertyType == typeof(LiveTable<T>));
            return propertyInfo == null ? null : propertyInfo.GetValue(this, null) as LiveTable<T>;
        }

        public DatabaseConnection DatabaseConnection(bool beginTransaction = false)
        {
            var ret = new DatabaseConnection(_connectionString);
            if (beginTransaction)
                ret.BeginTransaction();
            return ret;
        }

        public abstract void JoinTables();

        private IEnumerable<ILiveTable> Tables
        {
            get
            {
                return GetType()
                    .GetProperties()
                    .Where(p => p.PropertyType.GetGenericTypeDefinition() == typeof (LiveTable<>))
                    .Select(p => p.GetValue(this, null) as ILiveTable);
            }
        }

        public void ApplyChanges()
        {
            // get all the changes
            var tables = Tables.Select(table => new
                {
                    Changes = table.GetChanges(),
                    Table = table,
                })
                .Where(t => t.Changes.Any())
                .ToArray();
            if (tables.Length == 0)
                return;

            using (var connection = DatabaseConnection(true))
            {
                // prepare merges
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = string.Join(";\n", tables.Select(t => t.Table.PrepareMergeSql));
                    cmd.ExecuteNonQuery();
                }

                // upload data
                foreach (var table in tables)
                    table.Table.UploadMerge(connection, table.Changes);

                // complete merges
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = string.Join(";\n", tables.Select(table => table.Table.CompleteMergeSql));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void NotifyTableChange()
        {
            _timeline.NotifyOnQuantum(ApplyChanges);
        }
    }
}
