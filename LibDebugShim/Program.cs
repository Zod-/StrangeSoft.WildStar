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
using StrangeSoft.WildStar.Model;

namespace LibDebugShim
{
    class Program
    {
        static String livePath;
        static String liveDestination;
        static String ptrPath;
        static String ptrDestination;

        public static void StartTask(String path, String destination) {
            Task<WildstarAssets> liveTask = Task.Run(() => new WildstarAssets(new DirectoryInfo(path)));
            ExtractAddons(liveTask, destination);
        }

        public static void LiveThread() {
            StartTask(livePath, liveDestination);
        }

        public static void PTRThread() {
            StartTask(ptrPath, ptrDestination);
        }

        static void Main(string[] args)
        {
            livePath = args[0];
            liveDestination = args[1];
            StartTask(livePath, liveDestination);
        }

        public static void ExtractAddons(Task<WildstarAssets> task, String destination) {

            foreach (var obj in task.GetAwaiter().GetResult().RootDirectoryEntries.SelectMany(EnumerateFiles).Where(i => i.Name.EndsWith("lua", StringComparison.InvariantCultureIgnoreCase) || i.Name.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase))) {

                String path = destination + obj.Parent;
                Directory.CreateDirectory(path);
                try {
                    obj.ExtractTo(path);
                } catch (Exception ex) {
                    Console.WriteLine($"Failed to read file: {obj}");
                    if (IsDirectoryEmpty(path)) {
                        Directory.Delete(path);
                    }
                }
            }
        }
        public static bool IsDirectoryEmpty(string path) {
            return !Directory.EnumerateFileSystemEntries(path).Any();
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
    }
}
