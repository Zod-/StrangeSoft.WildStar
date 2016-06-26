namespace StrangeSoft.WildStar.Archive
{
    public abstract class ArchiveEntry : IArchiveEntry
    {
        private readonly IArchiveDirectoryEntry _parent;
        private readonly int _blockNumber;
        protected WildstarFile Index { get; }
        public long ParentIndex { get; }

        protected ArchiveEntry(WildstarFile indexFile, WildstarAssets assets, IArchiveDirectoryEntry parent, int blockNumber, string name)
        {
            _parent = parent;
            _blockNumber = blockNumber;
            Index = indexFile;
            Assets = assets;
            Name = name;
        }

        public WildstarAssets Assets { get; set; }

        public int BlockNumber => _blockNumber;
        public string Name { get; }
        public abstract void ExtractTo(string folder, string name = null);

        private IArchiveDirectoryEntry Parent => _parent;

        protected long Offset => (long)BlockTableEntry.DirectoryOffset;
        protected long Size => (long)BlockTableEntry.BlockSize;
        protected BlockTableEntry BlockTableEntry => Index.BlockTable[_blockNumber];
        public abstract bool Exists { get; }
        public override string ToString()
        {
            var ret = Parent == null ? $"{Name}" : $"{Parent}/{Name}";
            return ret;
        }
    }
}