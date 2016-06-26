using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public class FileHeader
    {
        public static FileHeader Load(BinaryReader reader)
        {
            return new FileHeader()
            {
                Magic = reader.ReadUInt32(),
                Version = reader.ReadUInt32()
            };
        }

        public uint Version { get; set; }

        public uint Magic { get; set; }
    }
}