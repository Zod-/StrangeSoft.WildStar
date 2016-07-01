using System.Collections.Generic;
using System.Linq;

namespace StrangeSoft.WildStar
{
    public class MergedArchiveDirectoryEntry : IArchiveDirectoryEntry
    {
        private readonly List<IArchiveEntry> _children;
        private readonly List<IArchiveDirectoryEntry> _directoryEntries;
        private readonly string _name;
        private readonly IArchiveDirectoryEntry _parent;
        public WildstarAssets Assets { get; set; }

        public MergedArchiveDirectoryEntry(WildstarAssets assets, IEnumerable<IArchiveDirectoryEntry> directories = null, string name = null, IArchiveDirectoryEntry parent = null)
        {
            _name = name;
            _parent = parent;
            Assets = assets;
            _directoryEntries = directories?.ToList() ?? assets.RootDirectoryEntries.ToList();
            _children = GroupDirectories(_directoryEntries.SelectMany(i => i.Children)).ToList();
            if (_directoryEntries.Count == 1)
            {
                BlockNumber = _directoryEntries[0].BlockNumber;
                IndexFile = _directoryEntries[0].IndexFile;
            }
        }

        public int BlockNumber { get; private set; } = -1;
        public string Name => _name;
        public void ExtractTo(string folder, string name = null, bool raw = false)
        {
            foreach (var rootDir in _directoryEntries)
            {
                rootDir.ExtractTo(folder, name, raw);
            }
        }

        public bool Exists => true;
        public WildstarFile IndexFile { get; private set; }
        public IArchiveDirectoryEntry Parent => _parent;
        public IEnumerable<IArchiveEntry> Children => _children;

        private IEnumerable<IArchiveEntry> GroupDirectories(IEnumerable<IArchiveEntry> entries)
        {
            var allEntries = entries.ToList();
            var directories = allEntries.OfType<IArchiveDirectoryEntry>();
            var files = allEntries.OfType<IArchiveFileEntry>();
            return directories.GroupBy(i => i.Name).Select(i => new MergedArchiveDirectoryEntry(Assets, i, i.Key, this)).Cast<IArchiveEntry>().Union(files);
        }

        public override string ToString()
        {
            return Parent == null ? $"{Name}" : $"{Parent}/{Name}";
        }
    }
}