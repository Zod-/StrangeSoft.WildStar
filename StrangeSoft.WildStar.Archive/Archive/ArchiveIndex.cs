using System.IO;

namespace StrangeSoft.WildStar
{
    public class ArchiveIndex : ArchiveBlockDescriptor
    {
        public uint Version { get; private set; }
        public uint Reserved { get; private set; }
        public uint RootBlock { get; private set; }

        internal override void Populate(uint magic, BinaryReader reader)
        {
            base.Populate(magic, reader);
            Version = reader.ReadUInt32();
            Reserved = reader.ReadUInt32();
            RootBlock = reader.ReadUInt32();
        }
    }
}