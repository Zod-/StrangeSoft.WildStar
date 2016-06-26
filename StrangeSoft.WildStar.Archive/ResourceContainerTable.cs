using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StrangeSoft.WildStar.Archive
{

    public class ResourceContainerTable : IEnumerable<ArchiveResourceEntry>
    {
        private readonly List<ArchiveResourceEntry> _entries = new List<ArchiveResourceEntry>();
        public ResourceContainerTable(AssetArchiveResourceContainerDescriptor descriptor, WildstarFile archiveFile)
        {
            archiveFile.BaseStream.Seek(archiveFile.BlockTable[descriptor.BlockEntry].DirectoryOffset, SeekOrigin.Begin);
            for (var x = 0; x < descriptor.EntryCount; x++)
            {
                var next = ArchiveResourceEntry.Load(archiveFile.BaseReader);
                var position = _entries.BinarySearch(next);
                if (position < 0)
                {
                    position = ~position;
                }
                _entries.Insert(position, next);
            }
        }

        public ArchiveResourceEntry Lookup(string hash)
        {
            var searchObject = new ArchiveResourceEntry()
            {
                Hash = hash
            };

            var position = _entries.BinarySearch(searchObject);
            if (position < 0) return null;
            return _entries[position];
        }

        public IEnumerator<ArchiveResourceEntry> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}