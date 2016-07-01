using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace StrangeSoft.WildStar
{
    public class BlockTable : IEnumerable<BlockTableEntry>
    {
        private readonly List<BlockTableEntry> _blockTableEntries;
        public BlockTable(IEnumerable<BlockTableEntry> blockTableEntries)
        {
            _blockTableEntries = blockTableEntries.ToList();
        }

        public static BlockTable Load(WildstarFile file, int directoryCountOffset = 544, int directoryStartOffset = 536)
        {
            List<BlockTableEntry> tableEntries = new List<BlockTableEntry>();
            var startPos = Math.Min(directoryCountOffset, directoryStartOffset);
            var endPos = Math.Max(directoryCountOffset + 4, directoryStartOffset + 8);
            int directoryCount;
            long directoryStart;
            var countOffset = directoryCountOffset - startPos;
            var startOffset = directoryStartOffset - startPos;
            using (var blockTableDataStream = file.File.CreateViewStream(startPos, endPos - startPos + 1, MemoryMappedFileAccess.Read))
            using (var binaryReader = new BinaryReader(blockTableDataStream))
            {
                binaryReader.BaseStream.Seek(startOffset, SeekOrigin.Begin);
                directoryStart = binaryReader.ReadInt64();
                binaryReader.BaseStream.Seek(countOffset, SeekOrigin.Begin);
                directoryCount = binaryReader.ReadInt32();
            }
            using (var directoryEntryStream = file.File.CreateViewStream(directoryStart, directoryCount * BlockTableEntry.Size, MemoryMappedFileAccess.Read))
            using (var binaryReader = new BinaryReader(directoryEntryStream))
                for (var x = 0; x < directoryCount; x++)
                {
                    tableEntries.Add(BlockTableEntry.FromReader(binaryReader));
                }

            return new BlockTable(tableEntries);
        }

        public IEnumerator<BlockTableEntry> GetEnumerator()
        {
            return _blockTableEntries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _blockTableEntries.Count;

        public BlockTableEntry this[int index] => _blockTableEntries[index];
    }
}