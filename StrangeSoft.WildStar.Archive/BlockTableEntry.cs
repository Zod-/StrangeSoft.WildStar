using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public class BlockTableEntry
    {
        public long DirectoryOffset { get; set; }
        public long BlockSize { get; set; }

        public static BlockTableEntry FromReader(BinaryReader reader)
        {
            var ret = new BlockTableEntry
            {
                DirectoryOffset = reader.ReadInt64(),
                BlockSize = reader.ReadInt64()
            };
            return ret;
        }
    }
}