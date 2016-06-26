using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StrangeSoft.WildStar.Archive
{
    public class BlockTable : IEnumerable<BlockTableEntry>
    {
        private readonly List<BlockTableEntry> _blockTableEntries;
        public BlockTable(IEnumerable<BlockTableEntry> blockTableEntries)
        {
            _blockTableEntries = blockTableEntries.ToList();
        }

        public static BlockTable Load(BinaryReader binaryReader, int directoryCountOffset = 544, int directoryStartOffset = 536)
        {
            List<BlockTableEntry> tableEntries = new List<BlockTableEntry>();
            var initialPosition = binaryReader.BaseStream.Position;
            binaryReader.BaseStream.Seek(directoryCountOffset, SeekOrigin.Begin);
            var directoryCount = binaryReader.ReadInt32();
            binaryReader.BaseStream.Seek(directoryStartOffset, SeekOrigin.Begin);
            var directoryStart = binaryReader.ReadInt64();
            binaryReader.BaseStream.Seek(directoryStart, SeekOrigin.Begin);
            for (var x = 0; x < directoryCount; x++)
            {
                tableEntries.Add(BlockTableEntry.FromReader(binaryReader));
            }

            binaryReader.BaseStream.Seek(initialPosition, SeekOrigin.Begin);

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