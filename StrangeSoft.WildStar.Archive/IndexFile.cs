using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace StrangeSoft.WildStar.Archive
{
    public class WildstarFile : IDisposable
    {
        readonly BinaryReader _binaryReader;
        private readonly Lazy<ArchiveIndex> _lazyAssetIndex;
        private Lazy<IReadOnlyList<ArchiveBlockDescriptor>> _lazyArchiveBlockDescriptors;
        private Lazy<ResourceContainerTable> _lazyAssetTable;
        public FileHeader FileHeader { get; }

        public BinaryReader BaseReader => _binaryReader;
        public Stream BaseStream => _binaryReader.BaseStream;

        public string Name { get; }

        public WildstarFile(FileInfo file) : this(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Path.GetFileNameWithoutExtension(file.Name)) { }
        public WildstarFile(Stream stream, string name) : this(new BinaryReader(stream), name) { }
        public WildstarFile(BinaryReader reader, string name)
        {
            Name = name;
            _binaryReader = reader;
            _binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
            FileHeader = FileHeader.Load(_binaryReader);
            if (FileHeader.Magic != Signatures.Pack) throw new InvalidDataException("The file signature does not match the expected signature, the file is not valid.");
            if (FileHeader.Version != 1) throw new InvalidOperationException($"This library only supports version 0x{1:X8}, but the file appears to be version 0x{FileHeader.Version:X8}");
            BlockTable = BlockTable.Load(_binaryReader);
            _lazyArchiveBlockDescriptors = new Lazy<IReadOnlyList<ArchiveBlockDescriptor>>(() => GetDescriptors(BlockTable, _binaryReader).ToList().AsReadOnly());
            _lazyAssetTable = new Lazy<ResourceContainerTable>(() => AssetArchiveResourceContainerDescriptor == null ? null : new ResourceContainerTable(AssetArchiveResourceContainerDescriptor, this));
        }

        public IReadOnlyList<ArchiveBlockDescriptor> BlockDescriptors => _lazyArchiveBlockDescriptors.Value;
        public ArchiveIndex AssetIndexEntry => BlockDescriptors.OfType<ArchiveIndex>().FirstOrDefault();
        public AssetArchiveResourceContainerDescriptor AssetArchiveResourceContainerDescriptor => BlockDescriptors.OfType<AssetArchiveResourceContainerDescriptor>().FirstOrDefault();
        public ResourceContainerTable AssetResourceTable => _lazyAssetTable.Value;


        private IEnumerable<ArchiveBlockDescriptor> GetDescriptors(BlockTable blockTable, BinaryReader binaryReader)
        {
            foreach (var entry in blockTable)
            {
                binaryReader.BaseStream.Seek(entry.DirectoryOffset, SeekOrigin.Begin);
                var magic = binaryReader.ReadUInt32();
                var blockDescriptor = ArchiveBlockDescriptor.Create(magic, binaryReader);
                if (blockDescriptor != null) yield return blockDescriptor;
            }
        }




        public BlockTable BlockTable { get; }


        public void Dispose()
        {
            _binaryReader.Dispose();
        }


    }

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

    //public class IndexFile : IDisposable
    //{
    //    List<Stream> _indexStreamList = new List<Stream>();
    //    List<Stream> _archiveStreamList = new List<Stream>();
    //    private readonly Stream _stream;
    //    private readonly bool _closeUnderlyingStreamOnDispose;



    //    private IndexFile(DirectoryInfo directory)
    //    {
    //        if (!directory.Exists) throw new DirectoryNotFoundException();
    //        var patchLocation = directory.EnumerateFileSystemInfos("Patch*", SearchOption.TopDirectoryOnly).ToArray();
    //        if (patchLocation.Length != 1) throw new DirectoryNotFoundException();
    //        if (patchLocation.Single() is DirectoryInfo)
    //        {
    //            patchLocation[0] as DirectoryInfo
    //            }
    //        _stream = stream;
    //        _archiveStream = archiveStream;
    //        _closeUnderlyingStreamOnDispose = closeUnderlyingStreamOnDispose;
    //        _indexBlockTableLazy = new Lazy<BlockTable>(LoadIndexBlockTable);
    //        _archiveBlockTableLazy = new Lazy<BlockTable>(LoadArchiveBlockTable);
    //        _lazyArchiveIndex = new Lazy<ArchiveIndex>(LoadEntryIndex);
    //        _lazyIndexBlockDescriptors = new Lazy<List<ArchiveBlockDescriptor>>(() => GetDescriptors(IndexBlockTableEntries, _stream).ToList());
    //        _lazyArchiveBlockDescriptors = new Lazy<List<ArchiveBlockDescriptor>>(() => GetDescriptors(ArchiveBlockTable, _archiveStream).ToList());
    //        _lazyAdvancedArchiveResourceContainer = new Lazy<AssetArchiveResourceContainerDescriptor>(() => _lazyArchiveBlockDescriptors.Value.OfType<AssetArchiveResourceContainerDescriptor>().Single());
    //        _lazyResourceTable = new Lazy<ResourceContainerTable>(() => new ResourceContainerTable(_lazyAdvancedArchiveResourceContainer.Value, this, ArchiveStream));
    //        LoadDirectoryEntries();
    //        //var blockDescriptors = _lazyArchiveBlockDescriptors.Value;
    //        //foreach (var item in _lazyResourceTable.Value)
    //        //{
    //        //    Debug.WriteLine($"{item.Hash} - {item.BlockIndex} - {item.UncompressedSize}");
    //        //}
    //    }

    //    public ResourceContainerTable ResourceTable => _lazyResourceTable.Value;

    //    private BlockTable LoadArchiveBlockTable()
    //    {
    //        return LoadBlockTable(_archiveStream);
    //    }

    //    private BlockTable LoadIndexBlockTable()
    //    {
    //        return LoadBlockTable(_stream);
    //    }


    //    private BlockTable LoadBlockTable(Stream stream, uint magic = Signatures.Pack, uint version = 1)
    //    {
    //        lock (stream)
    //        {
    //            using (var binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
    //            {
    //                stream.Seek(0, SeekOrigin.Begin);
    //                var header = FileHeader.Load(binaryReader);
    //                if (header.Magic != magic) throw new InvalidDataException("The file signature does not match the expected signature, the file is not valid.");
    //                if (header.Version != version) throw new InvalidOperationException($"This library only supports version 0x{1:X8}, but the file appears to be version 0x{version:X8}");
    //                return BlockTable.Load(binaryReader);
    //            }
    //        }
    //    }


    //    private void LoadDirectoryEntries()
    //    {
    //        var directoryEntry = new ArchiveDirectoryEntry(this, null, (int)EntryIndex.RootBlock, null);
    //        RootDirectory = directoryEntry;
    //    }

    //    private Lazy<BlockTable> _indexBlockTableLazy;
    //    private Lazy<BlockTable> _archiveBlockTableLazy;
    //    private Lazy<ArchiveIndex> _lazyArchiveIndex;
    //    private Stream _archiveStream;

    //    public IArchiveDirectoryEntry RootDirectory { get; private set; }

    //    public ArchiveIndex EntryIndex => _lazyArchiveIndex.Value;

    //    public BlockTable IndexBlockTableEntries => _indexBlockTableLazy.Value;
    //    public BlockTable ArchiveBlockTable => _archiveBlockTableLazy.Value;

    //    private IEnumerable<ArchiveBlockDescriptor> GetDescriptors(BlockTable blockTable, Stream stream)
    //    {
    //        using (var binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
    //        {
    //            foreach (var entry in blockTable)
    //            {
    //                _stream.Seek((long)entry.DirectoryOffset, SeekOrigin.Begin);
    //                var magic = binaryReader.ReadUInt32();
    //                var blockDescriptor = ArchiveBlockDescriptor.Create(magic, binaryReader);
    //                if (blockDescriptor != null) yield return blockDescriptor;
    //            }
    //        }
    //    }

    //    private Lazy<List<ArchiveBlockDescriptor>> _lazyIndexBlockDescriptors;
    //    private Lazy<List<ArchiveBlockDescriptor>> _lazyArchiveBlockDescriptors;

    //    private Lazy<AssetArchiveResourceContainerDescriptor> _lazyAdvancedArchiveResourceContainer;
    //    private Lazy<ResourceContainerTable> _lazyResourceTable;

    //    public IEnumerable<ArchiveBlockDescriptor> IndexBlockDescriptors => _lazyIndexBlockDescriptors.Value;

    //    private ArchiveIndex LoadEntryIndex()
    //    {

    //        var archiveIndex = IndexBlockDescriptors.OfType<ArchiveIndex>().FirstOrDefault();
    //        if (archiveIndex == null)
    //        {
    //            throw new InvalidOperationException("Could not find AIDX entry in file, file appears invalid!");
    //        }
    //        return archiveIndex;
    //    }

    //    public static IndexFile FromFileInfo(FileInfo file)
    //    {
    //        var archiveFileInfo = new FileInfo(Path.ChangeExtension(file.FullName, ".archive"));
    //        return Create(file, archiveFileInfo);

    //    }

    //    public static IndexFile Create(FileInfo indexFile, FileInfo archiveFile)
    //    {
    //        var ret = new IndexFile(indexFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), archiveFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), true);
    //        return ret;
    //    }

    //    public Stream IndexStream => _stream;
    //    public Stream ArchiveStream => _archiveStream;

    //    void IDisposable.Dispose()
    //    {
    //        Dispose(true);
    //    }

    //    public void Dispose(bool disposing)
    //    {
    //        if (_closeUnderlyingStreamOnDispose)
    //            _stream.Dispose();


    //    }
    //}
}
