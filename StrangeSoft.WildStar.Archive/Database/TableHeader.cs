using System.IO;
using System.Text;

namespace StrangeSoft.WildStar.Database
{
    public class TableHeader
    {
        public const uint ExpectedMagic = 0x4454424C;
        public uint Magic { get; set; }
        public int Version { get; set; }
        public long TableNameLength { get; set; }
        public long Unknown1 { get; set; }
        public long RecordSize { get; set; }
        public long FieldDescriptionCount { get; set; }
        public long FieldDescriptionOffset { get; set; }
        public long EntryCount { get; set; }
        public long EntryBlockSize { get; set; }
        public long EntryBlockOffset { get; set; }
        public long MaxEntry { get; set; }
        public long IdLookupOffset { get; set; }
        public long Unknown2 { get; set; }

        public long DataOffset { get; set; }

        public static TableHeader FromReader(BinaryReader reader)
        {
            var magic = reader.ReadUInt32();
            if (magic != ExpectedMagic)
                throw new InvalidDataException($"Expecting signature {ExpectedMagic:X8}, but got {magic:X8}");

            var ret = new TableHeader
            {
                Magic = magic,
                Version = reader.ReadInt32(),
                TableNameLength = reader.ReadInt64(),
                Unknown1 = reader.ReadInt64(),
                RecordSize = reader.ReadInt64(),
                FieldDescriptionCount = reader.ReadInt64(),
                FieldDescriptionOffset = reader.ReadInt64(),
                EntryCount = reader.ReadInt64(),
                EntryBlockSize = reader.ReadInt64(),
                EntryBlockOffset = reader.ReadInt64(),
                MaxEntry = reader.ReadInt64(),
                IdLookupOffset = reader.ReadInt64(),
                Unknown2 = reader.ReadInt64()

            };
            // We don't want the null character, hence the -1.
            ret.TableName = Encoding.Unicode.GetString(reader.ReadBytes(((int)ret.TableNameLength - 1) * 2));
            //uint64 offset = mFieldDescs.size() * sizeof(FieldDescEntry) + mHeader.ofsFieldDesc + 0x60;
            //if (offset % 16)
            //{
            //    offset += 16 - (offset % 16);
            //}

            ret.DataOffset = (ret.FieldDescriptionCount * TableFieldDescriptor.Size) + Size + ret.FieldDescriptionOffset;
            if (ret.DataOffset % 16 != 0)
                ret.DataOffset += 16 - (ret.DataOffset % 16);
            return ret;
        }

        public string TableName { get; set; }

        public const long Size = 0x60;
    }
}