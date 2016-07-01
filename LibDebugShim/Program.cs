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
using StrangeSoft.WildStar;
using StrangeSoft.WildStar.Database;

namespace LibDebugShim
{
    class Program
    {
        private const string PublicTestRealm = @"C:\Program Files (x86)\NCSOFT\WildStarPTR";
        private const string LiveRealms = @"C:\Program Files (x86)\Steam\steamapps\common\WildStar";
        private const string Target = PublicTestRealm;
        static void Main(string[] args)
        {
            //var table = new FileInfo(@"D:\WSData\Live\DB\Creature2.tbl").ToTable();
            //Console.WriteLine(string.Join(", ", table.TableFieldDescriptors));
            //foreach (var row in table.Rows.Select(i => string.Join(", ", i.Columns)))
            //{
            //    Console.WriteLine(row);
            //}
            ThreadPool.SetMaxThreads(16384, 16384);
            ThreadPool.SetMinThreads(200, 200);
            var liveTask = Task.Run(() => new WildstarAssets(new DirectoryInfo(LiveRealms)));
            //var publicTestTask = Task.Run(() => new WildstarAssets(new DirectoryInfo(PublicTestRealm)));

            foreach (
                var tableFile in
                    liveTask.GetAwaiter()
                        .GetResult()
                        .RootDirectoryEntries.SelectMany(i => i.Children)
                        .OfType<IArchiveDirectoryEntry>()
                        .Where(i => string.Equals(i.Name, "DB", StringComparison.InvariantCultureIgnoreCase))
                        .SelectMany(i => i.Children)
                        .OfType<IArchiveFileEntry>()
                        .Where(i => i.Name.EndsWith("tbl", StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.Write($"Exporting - {tableFile}");
                using (var tableObject = tableFile.ToTable())
                {
                    tableObject.DumpToCsv($@"D:\WSData\Live\DB\{tableObject.TableHeader.TableName}.csv");
                    Console.WriteLine(" - DONE!");
                }
            }

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

            //Func<ArchiveFileEntry, string> createCsvEntry = i => $"{i},{i.Flags},{(i.ExistsOnDisk ? "Disk" : "Archive")},{i.Hash},{i.CompressedSize},{i.UncompressedSize},{i.ArchiveFile?.Name},{i.Reserved1:X16},{i.Reserved2:X16},{i.TableEntry?.DirectoryOffset},{i.TableEntry?.BlockSize}";
            //Parallel.Invoke(() =>
            //{

            //    var publicTestAssets = publicTestTask.GetAwaiter().GetResult();
            //    using (
            //        var fileStream = File.CreateText($@"D:\WSData\filelist.ptr.{DateTimeOffset.Now:yyyyMMddhhmmss}.txt")
            //        )
            //    {
            //        fileStream.WriteLine(
            //            "Path,Flags,LocationType,Hash,CompressedSize,UncompressedSize,ArchiveName,Reserved1,Reserved2,DirectoryOffset,BlockSize");
            //        foreach (
            //            var entry in
            //                publicTestAssets.RootDirectoryEntries.SelectMany(EnumerateFiles)
            //                    .OfType<ArchiveFileEntry>()
            //                    .Select(i => createCsvEntry(i)))
            //        {
            //            fileStream.WriteLine(entry);
            //        }
            //    }
            //},
            //    () =>
            //    {
            //        var liveAssets = liveTask.GetAwaiter().GetResult();
            //        using (
            //            var fileStream =
            //                File.CreateText($@"D:\WSData\filelist.live.{DateTimeOffset.Now:yyyyMMddhhmmss}.txt")
            //            )
            //        {
            //            fileStream.WriteLine(
            //                "Path,Flags,LocationType,Hash,CompressedSize,UncompressedSize,ArchiveName,Reserved1,Reserved2,DirectoryOffset,BlockSize");
            //            foreach (
            //                var entry in
            //                    liveAssets.RootDirectoryEntries.SelectMany(EnumerateFiles)
            //                        .OfType<ArchiveFileEntry>()
            //                        .Select(i => createCsvEntry(i)))
            //            {
            //                fileStream.WriteLine(entry);
            //            }
            //        }
            //    });
            //foreach (var rootDir in liveTask.GetAwaiter().GetResult().RootDirectoryEntries)
            //    ExtractFiles(rootDir, @"D:\WSData\Live");

            //foreach (var rootDir in publicTestTask.GetAwaiter().GetResult().RootDirectoryEntries)
            //    ExtractFiles(rootDir, @"D:\WSData\PTR");
            //        Parallel.ForEach(liveAssets.RootDirectoryEntries,
            //rootDir => ExtractFiles(rootDir, @"D:\WSData\Live"));
            //Parallel.ForEach(publicTestAssets.RootDirectoryEntries,
            //            rootDir => ExtractFiles(rootDir, @"D:\WSData\PTR"));


            //Parallel.ForEach(assets.RootDirectoryEntries, rootDir =>
            //{
            //    ExtractFiles(rootDir, @"D:\WSData");
            //});

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
        private static readonly Semaphore ExtractSemaphore = new Semaphore(32, 32);
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
            Console.WriteLine($"Scanning {entry}");
            Interlocked.Increment(ref directoryCount);
            Directory.CreateDirectory(path);
            var children = entry.Children.ToList();
            var directories = children.OfType<IArchiveDirectoryEntry>();
            // ISSUE: Interface needs to be expanded so we don't need the actual class.
            var files = children.OfType<ArchiveFileEntry>();
            Parallel.Invoke(() =>
            {
                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        ExtractSemaphore.WaitOne();
                        try
                        {
                            Interlocked.Increment(ref fileCount);


                            if (file.ExistsOnDisk)
                                return;
                            if (!file.Exists)
                            {
                                Interlocked.Increment(ref notFoundCount);
                                return;
                            }
                            file.ExtractTo(path);
                        }
                        finally
                        {
                            ExtractSemaphore.Release();
                        }
                        //Console.WriteLine($"{file}");
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref decompressionFailedCount);
                        lock (failedFileList)
                            failedFileList.Add(file.ToString());
                        //Console.WriteLine("Failed to extract: {0} due to an exception: {1}", item, ex);
                        try
                        {
                            if (File.Exists(Path.Combine(path, file.Name)))
                            {
                                File.Delete(Path.Combine(path, file.Name));
                            }
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }
                });
            }, () =>
            {
                Parallel.ForEach(directories, directoryEntry =>
                {
                    //foreach (var directoryEntry in directories)
                    //{
                    ExtractFiles(directoryEntry, Path.Combine(path, directoryEntry.Name));
                    //}
                });
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
