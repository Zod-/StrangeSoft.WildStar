using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StrangeSoft.WildStar.Model
{
    //public class ModelHeader
    //{
    //    // 416
    //    public byte[] UnknownBytes1 { get; }
    //    public long TextureCount { get; }
    //    public long TextureOffset { get; }
    //    //32
    //    public byte[] UnknownBytes2 { get; }


    //}

    public sealed class M3Model : IDisposable
    {
        private readonly Stream _dataStream;
        public string Name { get; }

        public const uint HeaderMagic = 0x4D4F444C;
        public static readonly int HeaderSize = Marshal.SizeOf<ModelHeader>();
        public M3Model(FileInfo fileInfo)
            : this(fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), fileInfo.Name)
        {
            
        }
        public M3Model(Stream dataStream, string name)
        {
            if (!dataStream.CanSeek)
            {
                var tmp = new MemoryStream();
                dataStream.CopyTo(tmp);
                tmp.Seek(0, SeekOrigin.Begin);
                dataStream.Dispose();
            }
            _dataStream = dataStream;
            var header = _dataStream.ReadObject<ModelHeader>(0L);
            var vertOffset = header.VertexOffset + HeaderSize;
            _dataStream.Seek(vertOffset, SeekOrigin.Begin);

            for (var x = 0; x < header.VertexCount; x++)
            {
                var vertex = _dataStream.ReadObject<Vertex>();
            }

            _dataStream.Seek(header.ViewOffset + HeaderSize, SeekOrigin.Begin);
            for (var x = 0; x < header.ViewCount; x++)
            {
                var skin = _dataStream.ReadObject<Skin>();
            }

            Name = name;
        }




        public void Dispose()
        {
            _dataStream.Dispose();
        }
    }
}
