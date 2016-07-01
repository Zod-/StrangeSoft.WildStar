using System.Collections.ObjectModel;

namespace StrangeSoft.WildStar.Database
{
    public enum FieldType : uint
    {
        UInt32 = 3,
        Float = 4,
        Bool = 11,
        UInt64 = 20,
        StringTableOffset = 0x82,

        ForceDword = 0xFFFFFFFF
    }
}