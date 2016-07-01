namespace StrangeSoft.WildStar
{
    public interface IArchiveEntry
    {
        int BlockNumber { get; }
        string Name { get; }
        void ExtractTo(string folder, string name = null, bool raw = false);
        bool Exists { get; }
        WildstarFile IndexFile { get; }
        IArchiveDirectoryEntry Parent { get; }
    }
}