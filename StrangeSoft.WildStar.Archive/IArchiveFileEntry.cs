using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public interface IArchiveFileEntry : IArchiveEntry
    {
        int Flags { get; }
        long CompressedSize { get; }
        long UncompressedSize { get; }
        string Hash { get; }
        string OnDiskPath { get; }
        Stream Open(bool raw = false);
    }
}