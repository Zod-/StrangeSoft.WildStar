namespace StrangeSoft.WildStar.Archive
{
    public abstract class ArchiveEntry : IArchiveEntry
    {
        private readonly IArchiveDirectoryEntry _parent;
        private readonly int _blockNumber;
        protected IndexFile Index { get; }
        public long ParentIndex { get; }

        protected ArchiveEntry(IndexFile indexFile, IArchiveDirectoryEntry parent, int blockNumber, string name)
        {
            _parent = parent;
            _blockNumber = blockNumber;
            Index = indexFile;
            Name = name;
        }

        public int BlockNumber => _blockNumber;
        public string Name { get; }
        private IArchiveDirectoryEntry Parent => _parent;

        protected long Offset => (long)BlockTableEntry.DirectoryOffset;
        protected long Size => (long)BlockTableEntry.BlockSize;
        protected BlockTableEntry BlockTableEntry => Index.BlockTableEntries[_blockNumber];

        public override string ToString()
        {
            var ret = Parent == null ? $"{Name}" : $"{Parent}/{Name}";
            return ret;
        }
    }
}