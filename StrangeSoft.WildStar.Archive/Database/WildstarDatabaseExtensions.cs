using System.IO;

namespace StrangeSoft.WildStar.Database
{
    public static class WildstarDatabaseExtensions
    {
        public static WildstarTable ToTable(this IArchiveFileEntry fileEntry)
        {
            return new WildstarTable(fileEntry.Open());
        }

        public static WildstarTable ToTable(this FileInfo fileInfo)
        {
            return new WildstarTable(fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public static void DumpToCsv(this WildstarTable tableObject, string fileName)
        {
            using (
    var streamWriter =
        new StreamWriter(File.Open(fileName,   //$@"D:\WSData\Live\DB\{tableObject.TableHeader.TableName}.csv",
            FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                streamWriter.WriteLine(string.Join(",", tableObject.TableFieldDescriptors));
                foreach (var row in tableObject.Rows)
                {
                    streamWriter.WriteLine(string.Join(",", row.Columns));
                }
            }
        }

        public static void DumpToCsv(this FileInfo file, string fileName)
        {
            using (var table = file.ToTable())
            {
                table.DumpToCsv(fileName);
            }
        }

        public static void DumpToCsv(this IArchiveFileEntry file, string fileName)
        {
            using (var table = file.ToTable())
            {
                table.DumpToCsv(fileName);
            }
        }
    }
}