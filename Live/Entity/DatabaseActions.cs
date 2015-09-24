using System;
using System.Collections.Generic;
using System.Linq;

namespace Vertigo.Live
{
    public class DatabaseActions : SqlBulkCopyReader
    {
        public DatabaseActions(IEnumerable<string> columns, IEnumerable<Tuple<DatabaseActionType,object[]>> actions)
        {
            _columns = columns;
            _count = actions.Count();
            _enumerator = actions.GetEnumerator();
        }

        private readonly IEnumerable<string> _columns;
        private readonly IEnumerator<Tuple<DatabaseActionType, object[]>> _enumerator;
        private readonly int _count;

        public int Count
        {
            get { return _count; }
        }

        public override bool Read()
        {
            return _enumerator.MoveNext();
        }

        public override object GetValue(int i)
        {
            return i == 0 ?
                _enumerator.Current.Item1 :
                _enumerator.Current.Item2[i-1];
        }

        public override int FieldCount
        {
            get { return _columns.Count() + 1; }
        }

        public override int GetOrdinal(string name)
        {
            return name == "__Action__" ?
                0 :
                _columns.IndexOf(name) + 1;
        }
    }
}