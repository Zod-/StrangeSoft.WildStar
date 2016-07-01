using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        

        public M3Model(FileInfo fileInfo)
            : this(fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), fileInfo.Name)
        {
            
        }
        public M3Model(Stream dataStream, string name)
        {
            _dataStream = dataStream;
            Name = name;
        }




        public void Dispose()
        {
            _dataStream.Dispose();
        }
    }
}
