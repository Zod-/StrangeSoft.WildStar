using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace StrangeSoft.WildStar
{
    public class WildstarFile : IDisposable
    {
        readonly BinaryReader _binaryReader;
        private readonly Lazy<ArchiveIndex> _lazyAssetIndex;
        private Lazy<IReadOnlyList<ArchiveBlockDescriptor>> _lazyArchiveBlockDescriptors;
        private Lazy<ResourceContainerTable> _lazyAssetTable;
        public FileHeader FileHeader { get; }
        public MemoryMappedFile File { get; }

        public string Name { get; }

        public WildstarFile(FileInfo file) : this(MemoryMappedFile.CreateFromFile(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), null, 0, MemoryMappedFileAccess.Read, HandleInheritability.Inheritable, false), Path.GetFileNameWithoutExtension(file.Name)) { }

        public WildstarFile(MemoryMappedFile file, string name)
        {
            File = file;
            Name = name;
            using (var mappedStream = file.CreateViewStream(0, FileHeader.Size, MemoryMappedFileAccess.Read))
            using (var binaryReader = new BinaryReader(mappedStream))
            {
                FileHeader = FileHeader.Load(binaryReader);

                if (FileHeader.Magic != Signatures.Pack)
                    throw new InvalidDataException(
                        "The file signature does not match the expected signature, the file is not valid.");
                if (FileHeader.Version != 1)
                    throw new InvalidOperationException(
                        $"This library only supports version 0x{1:X8}, but the file appears to be version 0x{FileHeader.Version:X8}");
            }
            BlockTable = BlockTable.Load(this);
            _lazyArchiveBlockDescriptors = new Lazy<IReadOnlyList<ArchiveBlockDescriptor>>(() => GetDescriptors(BlockTable).ToList().AsReadOnly());
            _lazyAssetTable = new Lazy<ResourceContainerTable>(() => AssetArchiveResourceContainerDescriptor == null ? null : new ResourceContainerTable(AssetArchiveResourceContainerDescriptor, this));
        }

        public IReadOnlyList<ArchiveBlockDescriptor> BlockDescriptors => _lazyArchiveBlockDescriptors.Value;
        public ArchiveIndex AssetIndexEntry => BlockDescriptors.OfType<ArchiveIndex>().FirstOrDefault();
        public AssetArchiveResourceContainerDescriptor AssetArchiveResourceContainerDescriptor => BlockDescriptors.OfType<AssetArchiveResourceContainerDescriptor>().FirstOrDefault();
        public ResourceContainerTable AssetResourceTable => _lazyAssetTable.Value;


        private IEnumerable<ArchiveBlockDescriptor> GetDescriptors(BlockTable blockTable)
        {

            foreach (var entry in blockTable)
            {
                if (entry.BlockSize == 0) continue;
                using (var mappedStream = File.CreateViewStream(entry.DirectoryOffset, entry.BlockSize, MemoryMappedFileAccess.Read))
                using (var binaryReader = new BinaryReader(mappedStream))
                {
                    var magic = binaryReader.ReadUInt32();
                    var blockDescriptor = ArchiveBlockDescriptor.Create(magic, binaryReader);
                    if (blockDescriptor != null) yield return blockDescriptor;
                }
            }
        }




        public BlockTable BlockTable { get; }


        public void Dispose()
        {
            _binaryReader.Dispose();
        }


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
