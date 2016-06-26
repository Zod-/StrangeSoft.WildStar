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

            ReadIndexData();
            LoadEntryIndex();
            LoadDirectoryEntries();
        }


        private void LoadDirectoryEntries()
        {
            var directoryEntry = new ArchiveDirectoryEntry(this, null, (int)EntryIndex.RootBlock, null);
            RootDirectory = directoryEntry;
        }

        private List<BlockTableEntry> _blockTable = new List<BlockTableEntry>();
        private Stream _archiveStream;

        public IArchiveDirectoryEntry RootDirectory { get; private set; }

        public ArchiveIndex EntryIndex { get; private set; }

        public IReadOnlyList<BlockTableEntry> BlockTableEntries => _blockTable.AsReadOnly();


        private void ReadIndexData()
        {
            lock (_stream)
            {
                using (var binaryReader = new BinaryReader(_stream, Encoding.UTF8, true))
                {
                    _stream.Seek(0, SeekOrigin.Begin);
                    var signature = binaryReader.ReadUInt32();
                    if (signature != Signatures.Pack) throw new InvalidDataException("The file signature does not match the expected signature, the file is not valid.");
                    var version = binaryReader.ReadUInt32();
                    if (version != 1) throw new InvalidOperationException($"This library only supports version 0x{1:X8}, but the file appears to be version 0x{version:X8}");

                    _stream.Seek(544, SeekOrigin.Begin);
                    var directoryCount = binaryReader.ReadUInt32();
                    _stream.Seek(536, SeekOrigin.Begin);
                    var directoryTableStart = binaryReader.ReadUInt64();

                    Debug.WriteLine($"directoryCount: {directoryCount}, table offset: {directoryTableStart}");

                    _stream.Seek((long)directoryTableStart, SeekOrigin.Begin);


                    for (var x = 0; x < directoryCount; x++)
                    {
                        _blockTable.Add(BlockTableEntry.FromReader(binaryReader));

                        Debug.WriteLine($"Read directory header with offset: 0x{_blockTable[x].DirectoryOffset:X16}, block size: 0x{_blockTable[x].BlockSize:X16}");
                    }
                    // TODO

                }
            }
        }

        private void LoadEntryIndex()
        {
            using (var binaryReader = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                ArchiveIndex archiveIndex = new ArchiveIndex();
                foreach (var entry in _blockTable)
                {
                    if (entry.BlockSize < 16) continue;

                    _stream.Seek((long)entry.DirectoryOffset, SeekOrigin.Begin);
                    var aidx = ArchiveIndex.FromReader(binaryReader);
                    if (aidx.Magic == Signatures.ArchiveIndex)
                    {
                        archiveIndex = aidx;
                        break;
                    }
                }

                if (archiveIndex.Magic != Signatures.ArchiveIndex)
                {
                    throw new InvalidOperationException("Could not find AIDX entry in file, file appears invalid!");
                }

                EntryIndex = archiveIndex;
            }
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
