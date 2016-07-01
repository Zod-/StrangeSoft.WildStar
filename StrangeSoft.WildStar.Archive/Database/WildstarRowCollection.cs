using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StrangeSoft.WildStar.Database
{
    public class WildstarRowCollection : IEnumerable<WildstarTableRow>, IDisposable
    {
        private readonly WildstarTable _table;
        private readonly BinaryReader _reader;

        public WildstarRowCollection(WildstarTable table, BinaryReader reader)
        {
            _table = table;
            _reader = reader;
        }

        public WildstarTableRow this[int index]
        {
            get
            {
                if (index < 0 || index >= _table.TableHeader.EntryCount) throw new IndexOutOfRangeException();
                return WildstarTableRow.Load(_table.TableHeader, _table.TableFieldDescriptors.ToList(), index, _reader);
            }
        }

        public IEnumerator<WildstarTableRow> GetEnumerator()
        {
            for (var x = 0; x < _table.TableHeader.EntryCount; x++)
            {
                yield return this[x];
                //WildstarTableRow.Load(_table.TableHeader, _table.TableFieldDescriptors.ToList(), x, _reader);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}