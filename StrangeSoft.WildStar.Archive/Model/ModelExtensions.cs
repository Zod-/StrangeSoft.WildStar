using System.IO;

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
    }
}