namespace StrangeSoft.WildStar.Archive
{
    public interface IArchiveEntry
    {
        int BlockNumber { get; }
        string Name { get; }
    }
}