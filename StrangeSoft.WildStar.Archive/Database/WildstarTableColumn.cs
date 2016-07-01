using System.IO;
using System.Text;

namespace StrangeSoft.WildStar.Database
{
    public class WildstarTableColumn
    {
        public object Value { get; set; }

        public override string ToString()
        {
            if (Value is string)
            {
                return $"\"{Value}\"";
            }
            return Value?.ToString() ?? "<NULL>";
        }

        public static WildstarTableColumn Load(TableHeader header, int rowNumber, TableFieldDescriptor column, BinaryReader reader)
        {
            var ret = new WildstarTableColumn();
            var offset = TableHeader.Size + (header.RecordSize * rowNumber) + header.EntryBlockOffset + column.RowOffset;
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);

            switch (column.FieldType)
            {
                case FieldType.Bool:
                    var val = reader.ReadUInt32();
                    ret.Value = val != 0;
                    break;
                case FieldType.Float:
                    ret.Value = reader.ReadSingle();
                    break;
                case FieldType.UInt32:
                    ret.Value = reader.ReadUInt32();
                    break;
                case FieldType.UInt64:
                    ret.Value = reader.ReadUInt64();
                    break;
                case FieldType.StringTableOffset:
                    // wat?
                    //var data = 
                    //var entryOffset = data & 0x00000000FFFFFFFF;
                    var lowOffset = reader.ReadUInt32();
                    var highOffset = reader.ReadUInt32();


                    if (lowOffset > 0)
                    {
                        highOffset = lowOffset;
                        //var nextByte = reader.ReadInt32();
                    }
                    // TODO
                    var stringTableOffset = header.EntryBlockOffset + TableHeader.Size + (long)highOffset;
                    if (stringTableOffset < reader.BaseStream.Length)
                    {
                        reader.BaseStream.Seek(stringTableOffset, SeekOrigin.Begin);
                        using (
                            var stringReader = new StreamReader(reader.BaseStream, Encoding.Unicode, false, 1024, true))
                        {
                            StringBuilder valueBuilder = new StringBuilder();
                            while (true)
                            {
                                var next = stringReader.Read();
                                if (next == 0 || next == -1)
                                {
                                    break;
                                }
                                valueBuilder.Append((char) next);
                            }
                            ret.Value = valueBuilder.ToString();
                        }
                    }
                    else
                    {
                        ret.Value = null;
                    }
                    break;
            }
            return ret;

        }
    }
}