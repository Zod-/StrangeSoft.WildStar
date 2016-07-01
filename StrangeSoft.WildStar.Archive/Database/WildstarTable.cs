using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StrangeSoft.WildStar.Database
{
    

    public class WildstarTable : IDisposable
    {
        private readonly Stream _stream;

        public TableHeader TableHeader { get; }
        public IReadOnlyList<TableFieldDescriptor> TableFieldDescriptors { get; }

        public WildstarRowCollection Rows { get; }
        private const int BufferSize = 65536;
        public WildstarTable(Stream stream)
        {
            if (!stream.CanSeek)
            {
                var temp = new MemoryStream();
                stream.CopyTo(temp);
                stream.Dispose();
                stream = temp;
                stream.Seek(0, SeekOrigin.Begin);
            }
            // we'll be seeking around quite a bit, best to buffer.
            stream = stream as BufferedStream ?? stream as MemoryStream ?? (Stream)new BufferedStream(stream, BufferSize);
            _stream = stream;
            TableHeader = LoadHeader();
            using (BinaryReader reader = new BinaryReader(_stream, Encoding.UTF8, true))
                TableFieldDescriptors = TableFieldDescriptor.Load(TableHeader, reader).ToList().AsReadOnly();
            Rows = new WildstarRowCollection(this, new BinaryReader(_stream, Encoding.UTF8, true));
        }

        private TableHeader LoadHeader()
        {
            using (var binaryReader = new BinaryReader(_stream, Encoding.Unicode, true))
                return TableHeader.FromReader(binaryReader);
        }


        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}