using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public struct ArchiveIndex
    {
        public uint Magic;
        public uint Version;
        public uint Reserved;
        public uint RootBlock;

        public static ArchiveIndex FromReader(BinaryReader reader)
        {
            return new ArchiveIndex
            {
                Magic = reader.ReadUInt32(),
                Version = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32(),
                RootBlock = reader.ReadUInt32()
            };
        }
    }
}