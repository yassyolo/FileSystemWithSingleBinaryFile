using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystemWithSingleBinaryFile
{
    public class Directory
    {
        public int Id;
        public string Name = null!;
        public int ParentId;
        public List<int> Children = new();

        public Directory(int id, string name, int parentId)
        {
            Id = id;
            Name = name;
            ParentId = parentId;
        }
    }
}
