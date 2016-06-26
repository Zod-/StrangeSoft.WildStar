using System;
using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public class ArchiveFileEntry : ArchiveEntry, IArchiveFileEntry
    {
        

        public ArchiveFileEntry(IndexFile indexFile, IArchiveDirectoryEntry parent, int blockNumber, string name, BinaryReader reader) : base(indexFile, parent, blockNumber, name)
        {
            Flags = reader.ReadInt32();
            Reserved1 = reader.ReadInt64();
            UncompressedSize = reader.ReadInt64();
            CompressedSize = reader.ReadInt64();
            Hash = reader.ReadBytes(20);
            Reserved2 = reader.ReadInt32();
        }

        public int Flags { get; }
        public long Reserved1 { get; }
        public long CompressedSize { get; }
        public long UncompressedSize { get; }
        public byte[] Hash { get; }
        public int Reserved2 { get; }
        public Stream Open()
        {
            throw new NotImplementedException();
        }
    }
}