using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using SharpCompress.Compressor.LZMA;
using CompressionMode = System.IO.Compression.CompressionMode;
using DeflateStream = System.IO.Compression.DeflateStream;

namespace StrangeSoft.WildStar
{
    public class ArchiveFileEntry : ArchiveEntry, IArchiveFileEntry
    {


        public ArchiveFileEntry(WildstarFile indexFile, WildstarAssets assets, IArchiveDirectoryEntry parent, int blockNumber, string name, BinaryReader reader) : base(indexFile, assets, parent, blockNumber, name)
        {
            Flags = reader.ReadInt32();
            Reserved1 = reader.ReadInt64();
            UncompressedSize = reader.ReadInt64();
            CompressedSize = reader.ReadInt64();
            Hash = BitConverter.ToString(reader.ReadBytes(20)).Replace("-", "").ToLower();
            Reserved2 = reader.ReadInt32();
            _lazyArchiveFile = new Lazy<WildstarFile>(() => Assets.LocateArchiveWithAsset(Hash));
        }

        public int Flags { get; }
        public long Reserved1 { get; }
        public long CompressedSize { get; }
        public long UncompressedSize { get; }
        public string Hash { get; }
        public int Reserved2 { get; }
        public bool IsCompressed => Deflate | Rar;
        public bool Rar => (Flags & 4) == 4;
        public bool Deflate => (Flags & 2) == 2;
        public ArchiveResourceEntry ResourceEntry => ArchiveFile?.AssetResourceTable?.Lookup(Hash);
        public override bool Exists => ExistsInArchive || ExistsOnDisk;
        public bool ExistsInArchive => ResourceEntry != null;
        public bool ExistsOnDisk => DoesThisExistOnDisk();

        public string OnDiskPath => Path.Combine(GetPathComponents());

        private string[] GetPathComponents()
        {
            List<string> ret = new List<string>
            {
                Assets.BaseDirectory.FullName,
                IndexFile.Name
            };

            ret.AddRange(Parent.ToString().Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries));

            ret.Add(Name);
            return ret.ToArray();
        }

        private bool DoesThisExistOnDisk()
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(Assets.BaseDirectory.FullName, IndexFile.Name));
            if (!directoryInfo.Exists) return false;
            var fileInfo = new FileInfo(OnDiskPath);
            if (!fileInfo.Exists) return false;
            using (var sha1 = SHA1.Create())
            {
                using (var fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var computedHash = BitConverter.ToString(sha1.ComputeHash(fileStream)).Replace("-", "").ToLower();
                    if (computedHash == Hash) return true;
                }
            }
            return false;
        }

        public WildstarFile ArchiveFile => _lazyArchiveFile.Value;

        public BlockTableEntry TableEntry => Exists ? ArchiveFile?.BlockTable[ResourceEntry.BlockIndex] : null;
        public Stream Open(bool raw = false)
        {
            if (!Exists) throw new FileNotFoundException();
            if (ExistsInArchive)
                return ReadBlockData(TableEntry, raw);
            else
            {
                return File.Open(OnDiskPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }

        public string ToString(bool includeExtraData = false)
        {
            if (includeExtraData)
                return base.ToString() +
                       $" (Flags: {Flags}, Exists: {(Exists ? "Yes" : "No")}, Raw size: {CompressedSize}, Uncompressed Size: {UncompressedSize} AARC Data: {(ResourceEntry == null ? "N/A" : $"Hash: {ResourceEntry.Hash}, Block Number: {ResourceEntry.BlockIndex}, Uncompressed Size: {ResourceEntry.UncompressedSize}")})";
            else return base.ToString();
        }

        static object fileCreationLock = new object();
        static Guid sessionId = Guid.NewGuid();
        private Lazy<WildstarFile> _lazyArchiveFile;
        private MemoryMappedFile SourceFile => ArchiveFile.File;

        private Stream ReadBlockData(BlockTableEntry fileBlock, bool raw = false)
        {
            var ret = (Stream)SourceFile.CreateViewStream(fileBlock.DirectoryOffset, fileBlock.BlockSize, MemoryMappedFileAccess.Read);
            if (!raw)
            {
                if (Deflate)
                    ret = new DeflateStream(ret, CompressionMode.Decompress, false);
                if (Rar)
                {
                    byte[] properties = new byte[5];
                    ret = new BufferedStream(ret, 16777216);
                    ret.Read(properties, 0, properties.Length);
                    
                    ret = new LzmaStream(properties, ret, ret.Length - properties.Length, UncompressedSize, null, false);
                }
            }
            return ret;
        }


        public override void ExtractTo(string folder, string fileName = null, bool raw = false)
        {
            if (folder == null) throw new ArgumentNullException(nameof(folder));
            fileName = fileName ?? Name;
            using (var fileStream = Open(raw))
            {
                if (raw && Rar)
                {
                    fileName = fileName + $".{UncompressedSize}.lzma";
                }
                else if (raw && Deflate)
                {
                    fileName = fileName + $".{UncompressedSize}.z";
                }
                var targetFile = Path.Combine(folder, fileName);
                using (var targetStream = File.Open(targetFile, FileMode.Create, FileAccess.Write))
                {
                    fileStream.CopyTo(targetStream, 16777216);
                }

            }
        }
    }
}