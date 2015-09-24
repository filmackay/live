using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Vertigo
{
    public static partial class Extensions
    {
        public static Task BulkCopy<TEntity>(this Table<TEntity> table, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            return table.BulkCopy(entities,
                (typeof(TEntity).GetCustomAttributes(typeof(TableAttribute), false) as TableAttribute[])[0].Name,
                table.Context.Transaction as SqlTransaction);
        }

        public static Task BulkCopy<TEntity>(this Table<TEntity> table, IEnumerable<TEntity> entities, string tableName, SqlTransaction transaction)
            where TEntity : class
        {
            return Task.Factory.StartNew(() =>
            {
                var connection = table.Context.Connection as SqlConnection;
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, table.Context.Transaction as SqlTransaction)
                {
                    BulkCopyTimeout = 0,
                };

                var reader = new LinqBulkCopyReader<TEntity>(entities);

                foreach (var column in reader.ColumnMappingList)
                    bulk.ColumnMappings.Add(column.Key, column.Key);
                bulk.DestinationTableName = tableName;
                bulk.WriteToServer(reader);
            });
        }
    }

    public abstract class SqlBulkCopyReader : IDataReader
    {
        // derived must implement only these three
        public abstract bool Read();
        public abstract object GetValue(int i);
        public abstract int FieldCount { get; }

        // empty methods derived classes may want to implement
        public virtual int GetOrdinal(string name) { throw new NotImplementedException(); }
        public virtual void Close() { }
        public virtual void Dispose() { }
        public virtual object this[int i] { get { throw new NotImplementedException(); } }
        public virtual int Depth { get { throw new NotImplementedException(); } }
        public virtual bool IsClosed { get { throw new NotImplementedException(); } }
        public virtual int RecordsAffected { get { throw new NotImplementedException(); } }
        public virtual DataTable GetSchemaTable() { throw new NotImplementedException(); }
        public virtual bool NextResult() { throw new NotImplementedException(); }
        public virtual object this[string name] { get { throw new NotImplementedException(); } }
        public virtual bool GetBoolean(int i) { throw new NotImplementedException(); }
        public virtual byte GetByte(int i) { throw new NotImplementedException(); }
        public virtual long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) { throw new NotImplementedException(); }
        public virtual char GetChar(int i) { throw new NotImplementedException(); }
        public virtual long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) { throw new NotImplementedException(); }
        public virtual IDataReader GetData(int i) { throw new NotImplementedException(); }
        public virtual string GetDataTypeName(int i) { throw new NotImplementedException(); }
        public virtual DateTime GetDateTime(int i) { throw new NotImplementedException(); }
        public virtual decimal GetDecimal(int i) { throw new NotImplementedException(); }
        public virtual double GetDouble(int i) { throw new NotImplementedException(); }
        public virtual Type GetFieldType(int i) { throw new NotImplementedException(); }
        public virtual float GetFloat(int i) { throw new NotImplementedException(); }
        public virtual Guid GetGuid(int i) { throw new NotImplementedException(); }
        public virtual short GetInt16(int i) { throw new NotImplementedException(); }
        public virtual int GetInt32(int i) { throw new NotImplementedException(); }
        public virtual long GetInt64(int i) { throw new NotImplementedException(); }
        public virtual string GetName(int i) { throw new NotImplementedException(); }
        public virtual string GetString(int i) { throw new NotImplementedException(); }
        public virtual int GetValues(object[] values) { throw new NotImplementedException(); }
        public virtual bool IsDBNull(int i) { throw new NotImplementedException(); }
    }

    public class LinqBulkCopyReader<TEntity> : SqlBulkCopyReader
        where TEntity : class
    {
        public LinqBulkCopyReader(IEnumerable<TEntity> entities)
        {
            _Enumerator = entities.GetEnumerator();
            _ColumnMappingList = new List<ColumnMapping>();

            var entityType = entities.GetType().GetInterface("IEnumerable`1").GetGenericArguments()[0];
            _TableName = (entityType.GetCustomAttributes(typeof(TableAttribute), false) as TableAttribute[])[0].Name;

            var properties = entityType.GetProperties();
            for (int index = 0; index < properties.Length; index++)
            {
                var property = properties[index];
                var columns = property.GetCustomAttributes(typeof(ColumnAttribute), false) as ColumnAttribute[];
                foreach (var column in columns)
                {
                    if ((!column.DbType.Contains("IDENTITY")) &&
                        (!column.IsDbGenerated) &&
                        (!column.IsVersion))
                    {
                        var mapping = new ColumnMapping()
                                          {
                                              ColumnIndex = index,
                                              ColumnName = column.Name ?? property.Name
                                          };

                        if (property.PropertyType == typeof(XElement))
                            mapping.ColumnGetter = row =>
                                {
                                    var val = property.GetValue(row, null);
                                    if (val is XElement)
                                        return (val as XElement).CreateReader();
                                    return null;
                                };
                        else
                            mapping.ColumnGetter = row => property.GetValue(row, null);

                        _ColumnMappingList.Add(mapping);
                    }
                }
            }
        }

        private readonly IEnumerator<TEntity> _Enumerator;
        private readonly IList<ColumnMapping> _ColumnMappingList;
        private readonly string _TableName;

        public string TableName
        {
            get { return _TableName; }
        }

        public IEnumerable<string> Columns
        {
            get
            {
                return _ColumnMappingList.Select(column => column.ColumnName);
            }
        }

        public IDictionary<string, int> ColumnMappingList
        {
            get
            {
                return _ColumnMappingList.Select(column => new { column.ColumnName, column.ColumnIndex }).ToDictionary(x => x.ColumnName, y => y.ColumnIndex);
            }
        }

        public override bool Read()
        {
            return _Enumerator.MoveNext();
        }

        public override object GetValue(int i)
        {
            return _ColumnMappingList
                .Where(column => column.ColumnIndex == i)
                .Single()
                .ColumnGetter(_Enumerator.Current);
        }

        public override int FieldCount
        {
            get
            {
                return _ColumnMappingList.Count;
            }
        }

        public override int GetOrdinal(string name)
        {
            return _ColumnMappingList
                .Where(column => column.ColumnName == name || column.ColumnName == "[" + name + "]")
                .Single()
                .ColumnIndex;
        }

        private class ColumnMapping
        {
            public int ColumnIndex { get; set; }
            public string ColumnName { get; set; }
            public Func<object, object> ColumnGetter { get; set; }
        }
    }
}
