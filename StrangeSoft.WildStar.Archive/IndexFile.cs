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
    public class IndexFile : IDisposable
    {
        private readonly Stream _stream;
        private readonly bool _closeUnderlyingStreamOnDispose;

        private IndexFile(Stream stream, Stream archiveStream, bool closeUnderlyingStreamOnDispose = false)
        {
            _stream = stream;
            _archiveStream = archiveStream;
            _closeUnderlyingStreamOnDispose = closeUnderlyingStreamOnDispose;
            _indexBlockTableLazy = new Lazy<BlockTable>(LoadIndexBlockTable);
            _archiveBlockTableLazy = new Lazy<BlockTable>(LoadArchiveBlockTable);
            _lazyArchiveIndex = new Lazy<ArchiveIndex>(LoadEntryIndex);
            _lazyIndexBlockDescriptors = new Lazy<List<ArchiveBlockDescriptor>>(() => GetDescriptors(IndexBlockTableEntries, _stream).ToList());
            _lazyArchiveBlockDescriptors = new Lazy<List<ArchiveBlockDescriptor>>(() => GetDescriptors(ArchiveBlockTable, _archiveStream).ToList());
            LoadDirectoryEntries();
            var blockDescriptors = _lazyArchiveBlockDescriptors.Value;
        }

        private BlockTable LoadArchiveBlockTable()
        {
            return LoadBlockTable(_archiveStream);
        }

        private BlockTable LoadIndexBlockTable()
        {
            return LoadBlockTable(_stream);
        }


        private BlockTable LoadBlockTable(Stream stream, uint magic = Signatures.Pack, uint version = 1)
        {
            lock (stream)
            {
                using (var binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var header = FileHeader.Load(binaryReader);
                    if (header.Magic != magic) throw new InvalidDataException("The file signature does not match the expected signature, the file is not valid.");
                    if (header.Version != version) throw new InvalidOperationException($"This library only supports version 0x{1:X8}, but the file appears to be version 0x{version:X8}");
                    return BlockTable.Load(binaryReader);
                }
            }
        }


        private void LoadDirectoryEntries()
        {
            var directoryEntry = new ArchiveDirectoryEntry(this, null, (int)EntryIndex.RootBlock, null);
            RootDirectory = directoryEntry;
        }

        private Lazy<BlockTable> _indexBlockTableLazy;
        private Lazy<BlockTable> _archiveBlockTableLazy;
        private Lazy<ArchiveIndex> _lazyArchiveIndex;
        private Stream _archiveStream;

        public IArchiveDirectoryEntry RootDirectory { get; private set; }

        public ArchiveIndex EntryIndex => _lazyArchiveIndex.Value;

        public BlockTable IndexBlockTableEntries => _indexBlockTableLazy.Value;
        public BlockTable ArchiveBlockTable => _archiveBlockTableLazy.Value;

        private IEnumerable<ArchiveBlockDescriptor> GetDescriptors(BlockTable blockTable, Stream stream)
        {
            using (var binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                foreach (var entry in blockTable)
                {
                    _stream.Seek((long) entry.DirectoryOffset, SeekOrigin.Begin);
                    var magic = binaryReader.ReadUInt32();
                    var blockDescriptor = ArchiveBlockDescriptor.Create(magic, binaryReader);
                    if (blockDescriptor != null) yield return blockDescriptor;
                }
            }
        }

        private Lazy<List<ArchiveBlockDescriptor>> _lazyIndexBlockDescriptors;
        private Lazy<List<ArchiveBlockDescriptor>> _lazyArchiveBlockDescriptors;

        public IEnumerable<ArchiveBlockDescriptor> IndexBlockDescriptors => _lazyIndexBlockDescriptors.Value;

        private ArchiveIndex LoadEntryIndex()
        {

            var archiveIndex = IndexBlockDescriptors.OfType<ArchiveIndex>().FirstOrDefault();
            if (archiveIndex == null)
            {
                throw new InvalidOperationException("Could not find AIDX entry in file, file appears invalid!");
            }
            return archiveIndex;
        }

        public static IndexFile FromFileInfo(FileInfo file)
        {
            var archiveFileInfo = new FileInfo(Path.ChangeExtension(file.FullName, ".archive"));
            return Create(file, archiveFileInfo);

        }

        public static IndexFile Create(FileInfo indexFile, FileInfo archiveFile)
        {
            var ret = new IndexFile(indexFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), archiveFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), true);
            return ret;
        }

        public Stream IndexStream => _stream;
        public Stream ArchiveStream => _archiveStream;

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (_closeUnderlyingStreamOnDispose)
                _stream.Dispose();


        }
    }
}
