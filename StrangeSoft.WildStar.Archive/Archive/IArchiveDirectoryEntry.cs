using System.Collections.Generic;

namespace StrangeSoft.WildStar
{
    public interface IArchiveDirectoryEntry : IArchiveEntry
    {
        IEnumerable<IArchiveEntry> Children { get; }
    }
}