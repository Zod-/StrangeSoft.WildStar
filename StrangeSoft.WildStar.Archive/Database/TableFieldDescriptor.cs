using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StrangeSoft.WildStar.Database
{
    public class TableFieldDescriptor
    {
        // Title length maybe?
        public long TitleLength { get; set; }
        public long TitleOffset { get; set; }
        public FieldType FieldType { get; set; }
        public int Unknown2 { get; set; }
        public long RowOffset { get; set; }

        public override string ToString()
        {
            return $"{Title} - {FieldType:G}";
        }

        public string Title { get; set; }

        public static IEnumerable<TableFieldDescriptor> Load(TableHeader header, BinaryReader reader)
        {
            var fields = LoadInteral(header, reader).ToList();
            var baseOffset = 0;
            foreach (var field in fields)
            {
                field.RowOffset = baseOffset;
                switch (field.FieldType)
                {
                    case FieldType.UInt32:
                    case FieldType.Bool:
                    case FieldType.Float:
                        baseOffset += 4;
                        break;
                    case FieldType.UInt64:
                    case FieldType.StringTableOffset:
                        baseOffset += 8;
                        break;
                    default:
                        throw new InvalidDataException($"Unknown column type: {field.FieldType:X8}");
                }
                reader.BaseStream.Seek(header.DataOffset + field.TitleOffset, SeekOrigin.Begin);
                field.Title = Encoding.Unicode.GetString(reader.ReadBytes((int)(field.TitleLength)));
            }
            return fields;
        }
        private static IEnumerable<TableFieldDescriptor> LoadInteral(TableHeader header, BinaryReader reader)
        {
            reader.BaseStream.Seek(TableHeader.Size + header.FieldDescriptionOffset, SeekOrigin.Begin);
            for (var x = 0; x < header.FieldDescriptionCount; x++)
            {
                var next = LoadSingle(reader);
                yield return next;
            }
        }

        private static TableFieldDescriptor LoadSingle(BinaryReader reader)
        {
            return new TableFieldDescriptor
            {
                TitleLength = (reader.ReadInt64() - 1) * 2,
                TitleOffset = reader.ReadInt64(),
                FieldType = (FieldType)reader.ReadUInt32(),
                Unknown2 = reader.ReadInt32()
            };
        }

        public const int Size = 24;
    }
}
