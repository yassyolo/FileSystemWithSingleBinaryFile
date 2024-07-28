using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FileSystemWithSingleBinaryFile
{
    public class FileSystemMetadata
    {
        public int BlockSize;
        public int TotalBlocks;
        public int FreeBlocks;
        public int FileCount;
        public int DirectoryCount;
        public int RootDirectoryId;

        public FileSystemMetadata(int blockSize, int totalBlocks)
        {
            BlockSize = blockSize;
            TotalBlocks = totalBlocks;
            FreeBlocks = totalBlocks - 1;
            FileCount = 0;
            DirectoryCount = 1;
            RootDirectoryId = 0;
        }
    }
}
