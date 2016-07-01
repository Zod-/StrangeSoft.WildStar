using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrangeSoft.WildStar.Database
{
    public class WildstarDatabase : IEnumerable<WildstarTable>
    {
        private readonly IReadOnlyList<WildstarTable> _tables;

        public WildstarDatabase(IEnumerable<WildstarTable> tables)
        {
            _tables = tables.ToList().AsReadOnly();
        }

        public WildstarDatabase(IArchiveDirectoryEntry databaseDirectory) : this(databaseDirectory.Children.OfType<IArchiveFileEntry>().Where(i => i.Name.EndsWith(".tbl", StringComparison.InvariantCultureIgnoreCase)).Select(i => i.ToTable()))
        {

        }

        public WildstarDatabase(WildstarAssets assets) : this(assets.RootDirectory.Children.Where(i => string.Equals("DB", i.Name, StringComparison.InvariantCultureIgnoreCase)).OfType<IArchiveDirectoryEntry>().Single()) { }


        public IEnumerator<WildstarTable> GetEnumerator()
        {
            return ((IEnumerable<WildstarTable>) _tables.ToList()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}