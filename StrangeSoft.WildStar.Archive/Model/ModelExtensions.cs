using System.IO;
using System.Runtime.InteropServices;

namespace StrangeSoft.WildStar.Model
{
    public static class ModelExtensions
    {
        public static M3Model ToModel(this FileInfo file)
        {
            return new M3Model(file);
        }
        public static M3Model ToModel(this IArchiveFileEntry file)
        {
            return new M3Model(file.Open(), file.Name);
        }

        public static T ReadObject<T>(this Stream stream, long? offset = null, SeekOrigin? seekOrigin = null) where T : struct
        {
            if (offset != null)
            {
                seekOrigin = seekOrigin ?? SeekOrigin.Begin;
                stream.Seek(offset.Value, seekOrigin.Value);
            }

            var bytes = Marshal.SizeOf<T>();
            var buffer = new byte[bytes];
            stream.Read(buffer, 0, bytes);
            return buffer.MarshalToObject<T>();
        }

        public static T MarshalToObject<T>(this byte[] rawData) where T : struct
        {
            var pinnedRawData = GCHandle.Alloc(rawData,
                                               GCHandleType.Pinned);
            try
            {
                // Get the address of the data array
                var pinnedRawDataPtr = pinnedRawData.AddrOfPinnedObject();

                // overlay the data type on top of the raw data
                return (T)Marshal.PtrToStructure(pinnedRawDataPtr, typeof(T));
            }
            finally
            {
                // must explicitly release
                pinnedRawData.Free();
            }
        }
    }


    
}