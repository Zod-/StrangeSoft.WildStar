namespace StrangeSoft.WildStar.Archive
{
    public abstract class ArchiveEntry : IArchiveEntry
    {
        private readonly int _blockNumber;
        public WildstarFile IndexFile { get; }
        public long ParentIndex { get; }

        protected ArchiveEntry(WildstarFile indexFile, WildstarAssets assets, IArchiveDirectoryEntry parent, int blockNumber, string name)
        {
            Parent = parent;
            _blockNumber = blockNumber;
            IndexFile = indexFile;
            Assets = assets;
            Name = name;
        }

        public WildstarAssets Assets { get; set; }

        public int BlockNumber => _blockNumber;
        public string Name { get; }
        public abstract void ExtractTo(string folder, string name = null, bool raw = false);

        public IArchiveDirectoryEntry Parent { get; }

        protected long Offset => (long)BlockTableEntry.DirectoryOffset;
        protected long Size => (long)BlockTableEntry.BlockSize;
        protected BlockTableEntry BlockTableEntry => IndexFile.BlockTable[_blockNumber];
        public abstract bool Exists { get; }
        public override string ToString()
        {
            var ret = Parent == null ? $"{Name}" : $"{Parent}/{Name}";
            return ret;
        }
    }
}