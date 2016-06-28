using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using SharpCompress.Archive.Rar;
using SharpCompress.Compressor.LZMA;
using SharpCompress.Compressor.PPMd;
using CompressionMode = System.IO.Compression.CompressionMode;
using DeflateStream = System.IO.Compression.DeflateStream;

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
        public bool IsCompressed => Deflate | Rar;
        public bool Rar => (Flags & 4) == 4;
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

        private static string GetBlockFileName(string hash)
        {
            var fullPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "StrangeSoft", "Wildstar", $"{sessionId:N}")).FullName;
            return Path.Combine(fullPath, Path.ChangeExtension(hash, "tmp"));
        }

        public override string ToString()
        {
            return base.ToString() + $" (Flags: {Flags}, Exists: {(Exists ? "Yes" : "No")}, Raw size: {CompressedSize}, Uncompressed Size: {UncompressedSize} AARC Data: {(ResourceEntry == null ? "N/A" : $"Hash: {ResourceEntry.Hash}, Block Number: {ResourceEntry.BlockIndex}, Uncompressed Size: {ResourceEntry.UncompressedSize}")})";
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

        private Stream ReadBlockData(BlockTableEntry fileBlock, bool raw = false)
        {
            byte[] buffer = new byte[81920];
            var lengthToCopy = CompressedSize;
            Stream ret = new MemoryStream();

            // I am. So. Fucking. Dumb. These two lines were in the loop........
            var stream = Assets.LocateArchiveWithAsset(Hash).BaseStream;
            stream.Seek(fileBlock.DirectoryOffset, SeekOrigin.Begin);
            while (lengthToCopy > 0)
            {
                var nextBlock = lengthToCopy > buffer.Length ? buffer.Length : (int)lengthToCopy;
                var bytesRead = stream.Read(buffer, 0, nextBlock);
                ret.Write(buffer, 0, bytesRead);
                lengthToCopy -= bytesRead;
            }
            ret.Seek(0, SeekOrigin.Begin);
            if (!raw)
            {
                if (Deflate)
                    ret = new DeflateStream(ret, CompressionMode.Decompress, false);
                if (Rar)
                {
                    byte[] properties = new byte[5];
                    ret.Read(properties, 0, properties.Length);
                    // Now only about 16 or so files have failed, need to figure out why. (Still doing the art folder)
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
                    fileStream.CopyTo(targetStream, 16384);
                }

            }
        }
    }
}