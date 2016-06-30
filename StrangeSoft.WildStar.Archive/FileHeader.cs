using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public class FileHeader
    {
        public const long Size = sizeof (int) + sizeof (int);
        public static FileHeader Load(BinaryReader reader)
        {
            return new FileHeader()
            {
                Magic = reader.ReadUInt32(),
                Version = reader.ReadInt32()
            };
        }

        public int Version { get; set; }

        public uint Magic { get; set; }
    }
}