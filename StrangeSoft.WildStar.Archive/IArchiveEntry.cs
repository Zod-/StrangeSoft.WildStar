using System.Security.Cryptography.X509Certificates;

namespace StrangeSoft.WildStar.Archive
{
    public interface IArchiveEntry
    {
        int BlockNumber { get; }
        string Name { get; }
        void ExtractTo(string folder, string name = null, bool raw = false);
        bool Exists { get; }
        
    }
}