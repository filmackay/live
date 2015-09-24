using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Xml.Linq;

namespace Vertigo.Live
{
    public class DatabaseActionReader<TEntity> : SqlBulkCopyReader
        where TEntity : class, ILiveEntity<TEntity>, new()
    {
        public DatabaseActionReader(IEnumerable<DatabaseAction<TEntity>> entities)
        {
            var columnIndex = 0;
            _enumerator = entities.GetEnumerator();
            _columnMappings = new List<ColumnMapping>
            {
                new ColumnMapping
                {
                    ColumnIndex = columnIndex++,
                    ColumnName = "__Action__",
                    ColumnGetter = action => action.Type.ToString().ToUpper(),
                }
            };

            TableName = typeof(TEntity).CustomAttribute<TableAttribute>().Name;

            foreach (var property in typeof(TEntity)
                .GetProperties()
                .Select(p => new { Info = p, Column = p.CustomAttribute<ColumnAttribute>() })
                .Where(p => p.Column != null))
            {
                if ((!property.Column.DbType.Contains("IDENTITY")) &&
                    (!property.Column.IsDbGenerated) &&
                    (!property.Column.IsVersion))
                {
                    var mapping = new ColumnMapping
                    {
                        ColumnIndex = columnIndex,
                        ColumnName = property.Column.Name ?? property.Info.Name,
                    };

                    var propertyClosure = property;
                    if (property.Info.PropertyType == typeof(XElement))
                        mapping.ColumnGetter = action =>
                        {
                            var val = propertyClosure.Info.GetValue(action.Entity, null);
                            return val is XElement ? (val as XElement).CreateReader() : null;
                        };
                    else
                        mapping.ColumnGetter = action => (propertyClosure.Info.GetValue(action.Entity, null) as ILiveValue).Value;

                    _columnMappings.Add(mapping);
                }
                columnIndex++;
            }
        }

        private readonly IEnumerator<DatabaseAction<TEntity>> _enumerator;
        private readonly List<ColumnMapping> _columnMappings;
        public string TableName { get; private set; }

        public IEnumerable<string> Columns
        {
            get { return _columnMappings.Select(column => column.ColumnName); }
        }

        public IDictionary<string, int> ColumnMappingList
        {
            get { return _columnMappings.ToDictionary(column => column.ColumnName, column => column.ColumnIndex); }
        }

        public override bool Read()
        {
            return _enumerator.MoveNext();
        }

        public override object GetValue(int i)
        {
            var ret = _columnMappings
                .Where(column => column.ColumnIndex == i)
                .Single()
                .ColumnGetter(_enumerator.Current);
            return ret;
        }

        public override int FieldCount
        {
            get
            {
                return _columnMappings.Count;
            }
        }

        public override int GetOrdinal(string name)
        {
            return _columnMappings
                .Where(column => column.ColumnName == name || column.ColumnName == "[" + name + "]")
                .Single()
                .ColumnIndex;
        }

        private class ColumnMapping
        {
            public int ColumnIndex { get; set; }
            public string ColumnName { get; set; }
            public Func<DatabaseAction<TEntity>, object> ColumnGetter { get; set; }
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
}