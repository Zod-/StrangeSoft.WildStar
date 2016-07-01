using System;
using System.IO;

namespace StrangeSoft.WildStar
{
    public class ArchiveResourceEntry : IComparable<ArchiveResourceEntry>, IComparable<IArchiveFileEntry>
    {
        public int BlockIndex { get; set; }
        public string Hash { get; set; }
        public long UncompressedSize { get; set; }

        public static ArchiveResourceEntry Load(BinaryReader reader)
        {
            var ret = new ArchiveResourceEntry
            {
                BlockIndex = reader.ReadInt32(),
                Hash = BitConverter.ToString(reader.ReadBytes(20)).Replace("-", "").ToLower(),
                UncompressedSize = reader.ReadInt64()
            };
            return ret;
        }

        public int CompareTo(ArchiveResourceEntry other)
        {
            if (other?.Hash == null) return -1;
            return string.Compare(other.Hash, Hash, StringComparison.Ordinal);
        }

        public int CompareTo(IArchiveFileEntry other)
        {
            if (other == null) return -1;
            return string.Compare(other.Hash, Hash, StringComparison.Ordinal);
        }
    }
}