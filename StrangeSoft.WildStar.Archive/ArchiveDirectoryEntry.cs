using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StrangeSoft.WildStar.Archive
{
    public class ArchiveDirectoryEntry : ArchiveEntry, IArchiveDirectoryEntry
    {

        public IEnumerable<IArchiveEntry> Children => _children.Value;

        private readonly Lazy<List<IArchiveEntry>> _children;

        public ArchiveDirectoryEntry(IndexFile indexFile, IArchiveDirectoryEntry parent, int blockNumber, string name) : base(indexFile, parent, blockNumber, name)
        {
            _children = new Lazy<List<IArchiveEntry>>(ReadChildren);
        }

        private const int directorySize = 8;
        private const int fileSize = 56;

        private List<IArchiveEntry> ReadChildren()
        {
            List<IArchiveEntry> children = new List<IArchiveEntry>();
            using (var binaryReader = new BinaryReader(Index.IndexStream, Encoding.UTF8, true))
            {
                Index.IndexStream.Seek(Offset, SeekOrigin.Begin);
                var directoryCount = binaryReader.ReadUInt32();
                var fileCount = binaryReader.ReadUInt32();
                var dataSize = (directoryCount * directorySize) + (fileCount * fileSize);
                var stringSize = BlockTableEntry.BlockSize - 8 - dataSize;
                var currentPosition = Index.IndexStream.Position;
                Index.IndexStream.Seek(dataSize, SeekOrigin.Current);
                byte[] nameData = binaryReader.ReadBytes((int)stringSize);
                Index.IndexStream.Seek(currentPosition, SeekOrigin.Begin);


                for (var x = 0; x < directoryCount; x++)
                {
                    var nameOffset = binaryReader.ReadInt32();
                    var nextBlock = binaryReader.ReadInt32();
                    var name = ReadCString(nameData, nameOffset);
                    children.Add(new ArchiveDirectoryEntry(Index, this, nextBlock, name));
                }

                for (var x = 0; x < fileCount; x++)
                {
                    var nameOffset = binaryReader.ReadInt32();
                    var name = ReadCString(nameData, nameOffset);
                    children.Add(new ArchiveFileEntry(Index, this, BlockNumber, name, binaryReader));
                }
            }
            return children;
        }

        private string ReadCString(byte[] data, int offset)
        {
            StringBuilder ret = new StringBuilder();

            for (var x = offset; x < data.Length; x++)
            {
                if (data[x] == 0) break;
                ret.Append((char)data[x]);
            }
            return ret.ToString();
        }
    }
}