using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StrangeSoft.WildStar.Archive
{
    public class WildstarAssets
    {
        private List<WildstarFile> _fileList = new List<WildstarFile>();
        public IEnumerable<WildstarFile> IndexFiles => _fileList.Take(IndexCount);
        public IEnumerable<WildstarFile> ArchiveFiles => _fileList.Skip(IndexCount).Reverse();

        public IEnumerable<IArchiveDirectoryEntry> RootDirectoryEntries => IndexFiles.Select(i => new ArchiveDirectoryEntry(i, this, null, (int)i.AssetIndexEntry.RootBlock, null));
        public int IndexCount { get; }
        public DirectoryInfo BaseDirectory { get; }
        public DirectoryInfo PatchDirectory { get; }
        public WildstarAssets(DirectoryInfo baseDirectoryInfo)
        {
            BaseDirectory = baseDirectoryInfo;
            PatchDirectory = new DirectoryInfo(Path.Combine(baseDirectoryInfo.FullName, "Patch"));
            var patchFileName = Path.Combine(PatchDirectory.FullName, "Patch.index");

            var patchFile = new WildstarFile(new FileInfo(patchFileName));

            var directory = new ArchiveDirectoryEntry(patchFile, this, null, (int)patchFile.AssetIndexEntry.RootBlock, null);
            var filesToLoad = directory.Children.OfType<ArchiveFileEntry>().ToList();
            foreach (var fileName in filesToLoad)
            {
                var file = GetFile(fileName);
                if (file != null)
                {
                    _fileList.Add(file);
                }
            }
            IndexCount = _fileList.Count;
            var coreDataFileName = Path.Combine(PatchDirectory.FullName, "CoreData.archive");
            var coreDataFile = new WildstarFile(new FileInfo(coreDataFileName));
            _fileList.Add(coreDataFile);
            foreach (var fileName in filesToLoad.Select(i => Path.ChangeExtension(i.Name, ".archive")))
            {
                var file = GetFile(fileName);
                if (file != null)
                {
                    _fileList.Add(file);
                }
            }
        }

        public IEnumerable<IArchiveEntry> GetArchiveEntries()
        {
            return RootDirectoryEntries.SelectMany(GetArchiveEntries);
        }

        public IEnumerable<IArchiveEntry> GetArchiveEntries(IArchiveDirectoryEntry directory)
        {
            yield return directory;
            foreach (var child in directory.Children)
            {
                if (child is IArchiveDirectoryEntry)
                {
                    foreach (var innerChild in GetArchiveEntries(child as IArchiveDirectoryEntry))
                    {
                        yield return innerChild;
                    }
                    continue;
                }
                yield return child;
            }
        }

        private WildstarFile GetFile(string fileName, bool required = false)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(PatchDirectory.FullName, fileName));
            if(!fileInfo.Exists) if (required) throw new FileNotFoundException(); else { return null; }
            return new WildstarFile(fileInfo);
        }

        private WildstarFile GetFile(ArchiveFileEntry fileName, bool required = false)
        {
            if (!fileName.Exists)
                if (required) throw new FileNotFoundException();
                else return null;
            return new WildstarFile(fileName.Open(), Path.GetFileNameWithoutExtension(fileName.Name));
        }

        public WildstarFile LocateArchiveWithAsset(string hash)
        {
            return _fileList.FirstOrDefault(i => i.AssetResourceTable?.Lookup(hash) != null);
        }

        public ResourceContainerTable ResourceTable { get; set; }
    }
}