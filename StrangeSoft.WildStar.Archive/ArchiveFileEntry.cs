using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace StrangeSoft.WildStar.Archive
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
        }

        public int Flags { get; }
        public long Reserved1 { get; }
        public long CompressedSize { get; }
        public long UncompressedSize { get; }
        public string Hash { get; }
        public int Reserved2 { get; }
        public bool IsCompressed => Deflate | GZip;
        public bool GZip => (Flags & 4) == 4;
        public bool Deflate => (Flags & 2) == 2;
        public ArchiveResourceEntry ResourceEntry => Assets.LocateArchiveWithAsset(Hash)?.AssetResourceTable?.Lookup(Hash);
        public override bool Exists => ExistsInArchive || ExistsOnDisk;
        public bool ExistsInArchive => ResourceEntry != null;
        public bool ExistsOnDisk => DoesThisExistOnDisk();

        private string OnDiskPath => Path.Combine(Assets.BaseDirectory.FullName, Index.Name, Name);

        private bool DoesThisExistOnDisk()
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(Assets.BaseDirectory.FullName, Index.Name));
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

        public BlockTableEntry TableEntry => Exists ? Assets.LocateArchiveWithAsset(Hash)?.BlockTable[ResourceEntry.BlockIndex] : null;
        public Stream Open()
        {
            if(!Exists) throw new FileNotFoundException();
            if (ExistsInArchive)
                return ReadBlockData(TableEntry);
            else
            {
                return File.Open(OnDiskPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }

        private static string GetBlockFileName(string hash)
        {
            var fullPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "StrangeSoft", "Wildstar", $"{sessionId:N}")).FullName;
            return Path.Combine(fullPath, Path.ChangeExtension(hash, "tmp"));
        }

        public override string ToString()
        {
            return base.ToString() + $" (Flags: {Flags}, Exists: {(Exists ? "Yes" : "No")}, Raw size: {CompressedSize}, Uncompressed Size: {UncompressedSize} AARC Data: {(ResourceEntry == null ? "N/A" : $"Hash: {ResourceEntry.Hash}, Block Number: {ResourceEntry.BlockIndex}, Uncompressed Size: {ResourceEntry.UncompressedSize}" )})";
        }

        static object fileCreationLock = new object();
        static Guid sessionId = Guid.NewGuid();

        private void EnsureTempDataExists(BlockTableEntry fileBlock)
        {
            var tempFile = GetBlockFileName(Hash);
            if (!File.Exists(tempFile))
            {
                lock (fileCreationLock)
                {
                    if (!File.Exists(tempFile))
                        using (var fileStream = File.Open(tempFile, FileMode.Create))
                        {
                            var stream = Assets.LocateArchiveWithAsset(Hash).BaseStream;
                            stream.Seek(fileBlock.DirectoryOffset, SeekOrigin.Begin);
                            byte[] buffer = new byte[16384];
                            var bytesToRead = fileBlock.BlockSize;

                            while (bytesToRead > 0)
                            {
                                var read = stream.Read(buffer, 0, buffer.Length > bytesToRead ? (int)bytesToRead : buffer.Length);
                                if (read == 0) throw new EndOfStreamException("Unexpected end of stream");
                                fileStream.Write(buffer, 0, read);
                                bytesToRead -= read;
                            }
                        }
                }
            }
        }

        private Stream ReadBlockData(BlockTableEntry fileBlock)
        {
            var tempFile = GetBlockFileName(Hash);
            EnsureTempDataExists(fileBlock);
            var rawDataStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (Deflate)
                return new DeflateStream(rawDataStream, CompressionMode.Decompress, false);
            if (GZip)
                return new GZipStream(rawDataStream, CompressionMode.Decompress, false);
            return rawDataStream;
        }


        public override void ExtractTo(string folder, string fileName = null)
        {
            if(folder == null) throw new ArgumentNullException(nameof(folder));
            fileName = fileName ?? Name;
            using (var fileStream = Open())
            {
                var targetFile = Path.Combine(folder, fileName);
                using (var targetStream = File.Open(targetFile, FileMode.Create, FileAccess.Write))
                {
                    fileStream.CopyTo(targetStream);
                }
            }
        }
    }
}