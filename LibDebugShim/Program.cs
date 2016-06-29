using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
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
            //Console.WriteLine("Index files from patch.index:");
            //foreach (var indexFile in assets.IndexFiles)
            //{
            //    Console.WriteLine($"{indexFile.Name}.index");
            //}

            //foreach (var archiveFile in assets.ArchiveFiles)
            //{
            //    Console.WriteLine($"{archiveFile.Name}.archive");
            //}

            //Console.WriteLine("File list:");

            //foreach (var file in assets.GetArchiveEntries())
            //{
            //    Console.WriteLine(file);
            //}
            using(var fileStream = File.CreateText($@"D:\WSData\filelist.{DateTimeOffset.Now:yyyyMMddhhmmss}.txt"))
            foreach (var entry in assets.RootDirectoryEntries.SelectMany(EnumerateFiles).OfType<ArchiveFileEntry>().Select(i => $"{i} - {i.Flags} - {(i.ExistsOnDisk ? "Disk" : "Archive")} : {i.Hash}"))
            {
                fileStream.WriteLine(entry);
            }

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
            
            //var allFiles = EnumerateFiles(indexFile.RootDirectory).ToList();
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


            
            
            //foreach (var rootDir in assets.RootDirectoryEntries)
            //{
            //    ExtractFiles(rootDir, @"D:\WSData");
            //    //rootDir.ExtractTo(@"D:\WSData");
            //}
            
            //Console.WriteLine($"Found {directoryCount} directories, {fileCount} files. {decompressionFailedCount} files failed to decompress and {notFoundCount} files could not be located in the archive or on the filesystem.");
            //Console.WriteLine("Failed file listing");
            //foreach (var name in failedFileList)
            //{
            //    Console.WriteLine(name);
            //}

            //}
            //List<long> successFileSizes = new List<long>();
            //List<long> failedFileSizes = new List<long>();
            //foreach (var obj in assets.RootDirectoryEntries.SelectMany(EnumerateFiles).Where(i => i.Name.EndsWith("lua", StringComparison.InvariantCultureIgnoreCase)))
            //{
            //    try
            //    {
            //        obj.ExtractTo(@"D:\WSData\");
            //        continue;
            //    }
            //    catch(Exception ex)
            //    {
            //        failedFileSizes.Add(obj.CompressedSize);
            //        Console.WriteLine($"Failed to read file: {obj}");
            //    }


            //    //obj.ExtractTo(@"D:\WSData");
            //    //if (string.Equals(obj.Name, "Gacha.lua", StringComparison.InvariantCultureIgnoreCase))
            //    //{
            //    //    Console.WriteLine($"{obj}");
            //    //}
            //}
        }
        private static Semaphore _extractSemaphore = new Semaphore(8, 8);
        static List<string> failedFileList = new List<string>();
        static int directoryCount = 0, fileCount = 0, decompressionFailedCount = 0, notFoundCount = 0;
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
            Interlocked.Increment(ref directoryCount);
            Directory.CreateDirectory(path);
            Parallel.ForEach(entry.Children, (item, parallelLoopState) =>
            {
                try
                {

                    if (item is IArchiveDirectoryEntry)
                    {
                        var directoryEntry = item as IArchiveDirectoryEntry;
                        ExtractFiles(directoryEntry, Path.Combine(path, directoryEntry.Name));
                    }
                    else
                    {
                        _extractSemaphore.WaitOne();
                        try
                        {
                            Interlocked.Increment(ref fileCount);
                            var file = item as IArchiveFileEntry;

                            Console.CursorLeft = 0;
                            Console.CursorTop = 0;
                            Debug.Assert(file != null, "file != null");
                            //Console.Write($"{file.Name}                                 \n                                                           \n                                       ");
                            if (!file.Exists)
                            {
                                Interlocked.Increment(ref notFoundCount);
                                notFoundCount++;
                                Console.WriteLine($"ERROR: Missing file: {file}");
                                return;
                            }
                            try
                            {
                                file.ExtractTo(path, raw: true);
                            }
                            catch
                            {
                                // Ignored.
                            }
                            file.ExtractTo(path);
                        }
                        finally
                        {
                            _extractSemaphore.Release();
                        }
                        //Console.WriteLine($"{file}");
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref decompressionFailedCount);
                    lock(failedFileList)
                    failedFileList.Add(item.ToString());
                    //Console.WriteLine("Failed to extract: {0} due to an exception: {1}", item, ex);
                    try
                    {
                        if (File.Exists(Path.Combine(path, item.Name)))
                        {
                            File.Delete(Path.Combine(path, item.Name));
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            });
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
