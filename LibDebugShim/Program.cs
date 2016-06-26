using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StrangeSoft.WildStar.Archive;

namespace LibDebugShim
{
    class Program
    {
        static void Main(string[] args)
        {
            using (
                var indexFile =
                    IndexFile.FromFileInfo(
                        new FileInfo(@"C:\Program Files (x86)\Steam\steamapps\common\WildStar\Patch\clientdata.index")))
            {
                DateTimeOffset start = DateTimeOffset.Now;
                var allDirectories = GetAllDirectories(indexFile.RootDirectory).ToList();
                var taken = DateTimeOffset.Now - start;
                Console.WriteLine("Reading all data took: {0}", taken);

                Console.WriteLine("Found the following directories: ");
                foreach (var directory in allDirectories)
                {
                    Console.WriteLine(directory);
                }


                Console.WriteLine("Found the following files: ");
                foreach (var file in GetAllFiles(indexFile.RootDirectory))
                {
                    Console.WriteLine(file);
                }


            }
        }

        private static IEnumerable<IArchiveFileEntry> GetAllFiles(IArchiveDirectoryEntry directoryEntry)
        {
            foreach (var entry in directoryEntry.Children)
            {
                if (entry is IArchiveFileEntry)
                {
                    yield return entry as IArchiveFileEntry;
                    continue;
                }
                
                foreach (var innerEntry in GetAllFiles(entry as IArchiveDirectoryEntry))
                {
                    yield return innerEntry;
                }
            }

        }

        private static IEnumerable<IArchiveDirectoryEntry> GetAllDirectories(IArchiveDirectoryEntry rootDirectory)
        {
            yield return rootDirectory;
            foreach (var directory in rootDirectory.Children.OfType<IArchiveDirectoryEntry>().SelectMany(GetAllDirectories))
            {
                yield return directory;
            }
        }
    }
}
