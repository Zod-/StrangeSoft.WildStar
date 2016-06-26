using System.Diagnostics;
using System.IO;

namespace StrangeSoft.WildStar.Archive
{
    public abstract class ArchiveBlockDescriptor
    {
        public uint Magic { get; set; }

        internal virtual void Populate(uint magic, BinaryReader reader)
        {
            Magic = magic;
        }


        public static ArchiveBlockDescriptor Create(uint magic, BinaryReader reader)
        {
            ArchiveBlockDescriptor ret = null;
            switch (magic)
            {
                case Signatures.ArchiveIndex:
                    ret = new ArchiveIndex();
                    ret.Populate(magic, reader);
                    break;
                case Signatures.Pack:
                    ret = new PackDescriptor();
                    ret.Populate(magic, reader);
                    break;
                case Signatures.Archive:
                    ret = new AssetArchiveResourceContainerDescriptor();
                    ret.Populate(magic, reader);
                    break;
            }
            //if (ret == null)
            //    Debug.WriteLine($"WARN: Unknown magic: {magic:X8}");
            return ret;
        }
    }
}