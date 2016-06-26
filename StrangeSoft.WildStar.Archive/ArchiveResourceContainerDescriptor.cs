using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public class ArchiveResourceContainerDescriptor : ArchiveBlockDescriptor
    {
        public uint Version { get; set; }
        public int EntryCount { get; set; }
        public int TableBlock { get; set; }

        internal override void Populate(uint magic, BinaryReader reader)
        {
            base.Populate(magic, reader);
            Version = reader.ReadUInt32();
            EntryCount = reader.ReadInt32();
            TableBlock = reader.ReadInt32();
        }
    }
}