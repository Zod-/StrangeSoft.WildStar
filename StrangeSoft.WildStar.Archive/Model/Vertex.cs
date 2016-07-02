using System.Runtime.InteropServices;

namespace StrangeSoft.WildStar.Model
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public float X;
        public float Y;
        public float Z;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Indicies;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Normals;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Tangents;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Unknown;

        public short S;
        public short T;
        public short U;
        public short V;
    }
}