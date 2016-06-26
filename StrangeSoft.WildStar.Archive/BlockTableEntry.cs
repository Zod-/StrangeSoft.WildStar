using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public struct BlockTableEntry
    {
        public ulong DirectoryOffset;
        public ulong BlockSize;

        public static BlockTableEntry FromReader(BinaryReader reader)
        {
            var ret = new BlockTableEntry
            {
                DirectoryOffset = reader.ReadUInt64(),
                BlockSize = reader.ReadUInt64()
            };
            return ret;
        }
    }
}