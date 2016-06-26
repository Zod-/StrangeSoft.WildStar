using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using StrangeSoft.WildStar.Archive;

namespace LibDebugShim
{
    class Program
    {
        static void Main(string[] args)
        {
            WildstarAssets assets =
                new WildstarAssets(new DirectoryInfo(@"C:\Program Files (x86)\Steam\steamapps\common\WildStar"));
            Console.WriteLine("Index files from patch.index:");
            foreach (var indexFile in assets.IndexFiles)
            {
                Console.WriteLine($"{indexFile.Name}.index");
            }

            foreach (var archiveFile in assets.ArchiveFiles)
            {
                Console.WriteLine($"{archiveFile.Name}.archive");
            }

            //Console.WriteLine("File list:");

            //foreach (var file in assets.GetArchiveEntries())
            //{
            //    Console.WriteLine(file);
            //}


            //using (
            //    var indexFile =
            //        IndexFile.Create(
            //            new FileInfo(@"C:\Program Files (x86)\Steam\steamapps\common\WildStar\Patch\clientdata.index"), new FileInfo(@"C:\Program Files (x86)\Steam\steamapps\common\WildStar\Patch\clientdata.archive")))
            //{
            //    DateTimeOffset start = DateTimeOffset.Now;
            //    //var allDirectories = GetAllDirectories(indexFile.RootDirectory).ToList();
            //    //var taken = DateTimeOffset.Now - start;
            //    //Console.WriteLine("Reading all data took: {0}", taken);

            //    //Console.WriteLine("Found the following directories: ");
            //    //foreach (var directory in allDirectories)
            //    //{
            //    //    Console.WriteLine(directory);
            //    //}


            //    //Console.WriteLine("Found the following files: ");
            //    //foreach (var file in GetAllFiles(indexFile.RootDirectory))
            //    //{
            //    //    Console.WriteLine(file);
            //    //}
            //    var allFiles = EnumerateFiles(indexFile.RootDirectory).ToList();
            //    var foundCount = allFiles.Count(i => i.Exists);
            //    var notFoundCount = allFiles.Count(i => !i.Exists);
            //    var byHash = allFiles.OrderByDescending(i => i.Hash).TakeWhile(i => i.Exists).ToList();
            //    var byExtension = allFiles.Where(i => i.Exists).GroupBy(i => Path.GetExtension(i.Name)).OrderByDescending(i => i.Count()).ToList();
            //    var byBlock = allFiles.OrderBy(i => i.BlockNumber).TakeWhile(i => i.Exists).ToList();
            //    var bySize = allFiles.OrderByDescending(i => i.UncompressedSize).TakeWhile(i => i.Exists).ToList();
            //    var byCompressedSize = allFiles.OrderByDescending(i => i.CompressedSize).TakeWhile(i => i.Exists).ToList();

            //    foreach (var file in byHash)
            //    {
            //        Console.WriteLine(file);
            //    }

            
            //}

            foreach (var obj in assets.RootDirectoryEntries)
            {
                obj.ExtractTo(@"D:\WSData");
                //if (string.Equals(obj.Name, "Gacha.lua", StringComparison.InvariantCultureIgnoreCase))
                //{
                //    Console.WriteLine($"{obj}");
                //}
            }
        }

        private static IEnumerable<IArchiveFileEntry> EnumerateFiles(IArchiveDirectoryEntry directoryEntry)
        {
            foreach (var item in directoryEntry.Children)
            {
                var directory = item as IArchiveDirectoryEntry;
                if (directory != null)
                {
                    foreach (var inner in EnumerateFiles(directory))
                    {
                        yield return inner;
                    }
                    continue;
                }
                var file = item as IArchiveFileEntry;
                yield return file;
            }
        }

        private static void ExtractFiles(IArchiveDirectoryEntry entry, string path)
        {
            foreach (var item in entry.Children)
            {
                if (item is IArchiveDirectoryEntry)
                {
                    var directoryEntry = item as IArchiveDirectoryEntry;
                    ExtractFiles(directoryEntry, Path.Combine(path, directoryEntry.Name));
                }
                else
                {
                    var file = item as IArchiveFileEntry;
                    if (!file.Exists)
                    {
                        Console.WriteLine($"ERROR: Missing file: {file}");
                        continue;
                    }
                    Console.WriteLine($"{file}");
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
