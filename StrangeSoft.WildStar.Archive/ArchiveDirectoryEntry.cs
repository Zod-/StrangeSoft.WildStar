using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace StrangeSoft.WildStar.Archive
{
    public class ArchiveDirectoryEntry : ArchiveEntry, IArchiveDirectoryEntry
    {

        public IEnumerable<IArchiveEntry> Children => _children.Value;

        private readonly Lazy<List<IArchiveEntry>> _children;

        public ArchiveDirectoryEntry(WildstarFile indexFile, WildstarAssets assets, IArchiveDirectoryEntry parent, int blockNumber, string name) : base(indexFile, assets, parent, blockNumber, name)
        {
            _children = new Lazy<List<IArchiveEntry>>(ReadChildren);
        }

        private const int directorySize = 8;
        private const int fileSize = 56;

        private List<IArchiveEntry> ReadChildren()
        {
            List<IArchiveEntry> children = new List<IArchiveEntry>();
            int directoryCount;
            int fileCount;
            using (var headerStream = IndexFile.File.CreateViewStream(Offset, Size, MemoryMappedFileAccess.Read))
            using (var binaryReader = new BinaryReader(headerStream))
            {
                directoryCount = binaryReader.ReadInt32();
                fileCount = binaryReader.ReadInt32();

                var dataSize = (directoryCount * directorySize) + (fileCount * fileSize);
                var stringSize = Size - 8 - dataSize;
                headerStream.Seek(dataSize, SeekOrigin.Current);
                byte[] nameData = binaryReader.ReadBytes((int)stringSize);
                headerStream.Seek(8, SeekOrigin.Begin);


                for (var x = 0; x < directoryCount; x++)
                {
                    var nameOffset = binaryReader.ReadInt32();
                    var nextBlock = binaryReader.ReadInt32();
                    var name = ReadCString(nameData, nameOffset);
                    children.Add(new ArchiveDirectoryEntry(IndexFile, Assets, this, nextBlock, name));
                }

                for (var x = 0; x < fileCount; x++)
                {
                    var nameOffset = binaryReader.ReadInt32();
                    var name = ReadCString(nameData, nameOffset);
                    children.Add(new ArchiveFileEntry(IndexFile, Assets, this, BlockNumber, name, binaryReader));
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

        public override bool Exists => true;


        public override void ExtractTo(string folder, string name = null, bool raw = false)
        {
            name = name ?? Name;
            DirectoryInfo target = Directory.CreateDirectory(string.IsNullOrWhiteSpace(name) ? folder : Path.Combine(folder, name));

            foreach (var child in Children)
            {
                if (child.Exists)
                {
                    Debug.WriteLine($"Extracting {child}");
                    child.ExtractTo(target.FullName, raw: raw);
                }
                else
                {
                    Debug.WriteLine(
                        $"WARNING: Missing file: {child}, Not found in Resource table");
                }
            }
        }
    }
}