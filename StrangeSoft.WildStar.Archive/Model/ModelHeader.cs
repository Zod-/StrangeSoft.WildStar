using System.Runtime.InteropServices;

namespace StrangeSoft.WildStar.Model
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ModelHeader
    {
        public uint Magic;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 412)]
        public byte[] Unknown1;
        public long TextureCount;
        public long TextureOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Unknown2;
        public long MaterialCount;
        public long MaterialOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Unknown3;
        public long VertexCount;
        public long VertexOffset;
        public long IndexCount;
        public long IndexOffset;
        public long SubMeshCount;
        public long SubMeshOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Unknown4;
        public long ViewCount;
        public long ViewOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1000 - 24)]
        public byte[] Unknown5;
    }
}