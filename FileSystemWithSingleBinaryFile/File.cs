using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemWithSingleBinaryFile
{
    public class File
    {
        public int Id;
        public string Name = null!;
        public List<int> Blocks = new();
        public List<byte[]> BlockHashes = new(); //hash value result from hashing the data blocks

        public File(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
