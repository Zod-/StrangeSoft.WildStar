using System.Collections.Generic;

namespace StrangeSoft.WildStar.Archive
{
    public interface IArchiveDirectoryEntry : IArchiveEntry
    {
        IEnumerable<IArchiveEntry> Children { get; }
    }
}