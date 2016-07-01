using System.IO;

namespace StrangeSoft.WildStar
{
    public class AssetArchiveResourceContainerDescriptor : ArchiveBlockDescriptor
    {
        public uint Version { get; set; }
        public int EntryCount { get; set; }
        public int BlockEntry { get; set; }

        internal override void Populate(uint magic, BinaryReader reader)
        {
            base.Populate(magic, reader);
            Version = reader.ReadUInt32();
            EntryCount = reader.ReadInt32();
            BlockEntry = reader.ReadInt32();
        }
    }
}