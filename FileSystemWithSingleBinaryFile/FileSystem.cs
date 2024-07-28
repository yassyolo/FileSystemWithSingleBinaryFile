using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace FileSystemWithSingleBinaryFile
{
    public class FileSystem
    {
        public string FileSystemContainer;
        public FileSystemMetadata _FileSystemMetadata;
        public Dictionary<int, Directory> Directories = new();
        public Dictionary<int, File> Files = new();
        public byte[] Bitmap; //byte array that represents the free and used blocks in the file system
        public Dictionary<string, int> BlockHashToIndex = new();//represents the hash values of the blocks with their ids
        public int nextId; //holds the next free id for storing files or directories

        public FileSystem(string fileSystemContainer, int totalBlocks, int blockSize)
        {
            FileSystemContainer = fileSystemContainer;
            _FileSystemMetadata = new FileSystemMetadata(blockSize, totalBlocks);
            Bitmap = new byte[totalBlocks / 8];
            nextId = 1;
            Directory root = new Directory(_FileSystemMetadata.RootDirectoryId, "/", -1);
            Directories[root.Id] = root;
        }


        public void Save()//ensures that the information of the file is saved, as it saves the matadata, the structure of the directories and files and their blocks,
                          //ensures that when loaded the filesystem is saved
        {
            //opens the binary file to write, if it existes it write over it filemode.create
            using (BinaryWriter bw = new BinaryWriter(System.IO.File.Open(FileSystemContainer, FileMode.Create)))
            {
                bw.Write(_FileSystemMetadata.BlockSize);
                bw.Write(_FileSystemMetadata.TotalBlocks);
                bw.Write(_FileSystemMetadata.FreeBlocks);
                bw.Write(_FileSystemMetadata.FileCount);
                bw.Write(_FileSystemMetadata.DirectoryCount);
                bw.Write(_FileSystemMetadata.RootDirectoryId);

                bw.Write(Bitmap);
                bw.Write(Directories.Count());

                foreach (var directory in Directories.Values)
                {
                    bw.Write(directory.Id);
                    bw.Write(directory.Name);
                    bw.Write(directory.ParentId);
                    bw.Write(directory.Children.Count);
                    foreach (var directoryChild in directory.Children)
                    {
                        bw.Write(directoryChild);
                    }
                }

                bw.Write(Files.Count());
                foreach (var file in Files.Values)
                {
                    bw.Write(file.Id);
                    bw.Write(file.Name);
                    bw.Write(file.Blocks.Count());
                    foreach (var block in file.Blocks)
                    {
                        bw.Write(block);
                    }
                    bw.Write(file.BlockHashes.Count());
                    foreach (var blockHash in file.BlockHashes)
                    {
                        bw.Write(blockHash.Length);
                        bw.Write(blockHash);
                    }
                }
                bw.Write(BlockHashToIndex.Count());
                foreach (var blockHashToIndex in BlockHashToIndex)
                {
                    bw.Write(blockHashToIndex.Key);
                    bw.Write(blockHashToIndex.Value);
                }
            }
        }

        public void Load()//ensures the correct loading of the file system
        {
            //opens the binary file for reading
            using (BinaryReader br = new BinaryReader(System.IO.File.Open(FileSystemContainer, FileMode.Open)))
            {
                _FileSystemMetadata.TotalBlocks = br.ReadInt32();
                _FileSystemMetadata.FreeBlocks = br.ReadInt32();
                _FileSystemMetadata.BlockSize = br.ReadInt32();
                _FileSystemMetadata.FileCount = br.ReadInt32();
                _FileSystemMetadata.DirectoryCount = br.ReadInt32();
                _FileSystemMetadata.RootDirectoryId = br.ReadInt32();

                Bitmap = br.ReadBytes(_FileSystemMetadata.TotalBlocks / 8);

                int directoriesCount = br.ReadInt32();
                Directories = new Dictionary<int, Directory>();
                for (int i = 0; i < directoriesCount; i++)
                {
                    int id = br.ReadInt32();
                    string name = br.ReadString();
                    int parentId = br.ReadInt32();
                    var directory = new Directory(id, name, parentId);
                    int childrensCount = br.ReadInt32();
                    for (int j = 0; j < childrensCount; j++)
                    {
                        int childId = br.ReadInt32();
                        directory.Children.Add(childId);
                    }
                    Directories[directory.Id]= directory;
                }

                int filesCount = br.ReadInt32();
                Files = new Dictionary<int, File>();
                for (int i = 0; i < filesCount; i++)
                {
                    int id = br.ReadInt32();
                    string name = br.ReadString();
                    var file = new File(id, name);
                    int fileBlocksCount = br.ReadInt32();
                    for (int j = 0; j < fileBlocksCount; j++)
                    {
                        int fileBlock = br.ReadInt32();
                        file.Blocks.Add(fileBlock);
                    }
                    int hashCount = br.ReadInt32();
                    for (int k = 0; k < hashCount; k++)
                    {
                        int hashLength = br.ReadInt32();
                        byte[] hash = br.ReadBytes(hashLength);
                        file.BlockHashes.Add(hash);
                    }
                    Files[file.Id] = file;
                }

                int hashCountToIndex = br.ReadInt32();
                for (int i = 0; i < hashCountToIndex; i++)
                {
                    string key = br.ReadString();
                    int value = br.ReadInt32();
                    BlockHashToIndex[key] = value;
                }
            }
        }
        public int FindDirectory(string name, int parentId)//method to find directory id by its name and parent id
        {
            foreach (var childId in Directories[parentId].Children)
            {
                if (Directories.ContainsKey(childId) && Directories[childId].Name == name)
                {
                    return childId;
                }
            }
            return -1;
        }
        public int FindFile(string name, int parentId)//method for finding a file with given name and parent id of the directory
        {
            foreach (var fileId in Directories[parentId].Children)
            {
                if (Files.ContainsKey(fileId) && Files[fileId].Name==name)
                {
                    return fileId;
                }
            }
            return -1;
        }
        public void CreateDirectory(string name)//method for creating new directory with given name
        {
            Directory parent = Directories[_FileSystemMetadata.RootDirectoryId];
            Directory newDirectory = new Directory(nextId++, name, parent.Id);
            Directories[newDirectory.Id] = newDirectory;
            parent.Children.Add(newDirectory.Id);
            _FileSystemMetadata.DirectoryCount++;
            Save();
        }
        public void DeleteEmptyDirectory(string name)
        {
            int directoryId = FindDirectory(name, _FileSystemMetadata.RootDirectoryId);
            if (directoryId == -1)
            {
                Console.WriteLine("Empty directory");
                return;
            }
            Directories.Remove(directoryId);
            _FileSystemMetadata.DirectoryCount--;
            Save();
        }
        public void ListDirectoryContent()//method for listing the contents of the current directory
        {
            Directory root = Directories[_FileSystemMetadata.RootDirectoryId];
            foreach (var childId in root.Children)
            {
                if (Directories.ContainsKey(childId))
                {
                    Console.WriteLine("Directory" + Directories[childId].Name);
                }
                if (Files.ContainsKey(childId))
                {
                    Console.WriteLine("File" + Files[childId].Name);
                }
            }
        }
        public void ListFileContent(string fileName)//list the contents of a file
        {
            var fileId = FindFile(fileName, _FileSystemMetadata.RootDirectoryId);
            if (fileId == -1)
            {
                Console.WriteLine("No such file exists.");
                return;
            }
            File file = Files[fileId];
            foreach (var block in file.Blocks)
            {
                byte[] data = ReadFromFile(fileId);
                Console.WriteLine(Encoding.UTF8.GetString(data));
            }
        }

        private byte[] ReadFromFile(int fileId)
        {
            File file = Files[fileId];
            List<byte> fileData = new();
            foreach (var block in file.Blocks)
            {
                byte[] blockData = ReadBlockFromFile(block);
                fileData.AddRange(blockData);
            }
            return fileData.ToArray();
        }

        private byte[] ReadBlockFromFile(int blockId)
        {
            byte[] data = new byte[_FileSystemMetadata.BlockSize];
            using (var stream = new FileStream(FileSystemContainer, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(_FileSystemMetadata.BlockSize * blockId, SeekOrigin.Begin);
                stream.Read(data, 0,data.Length);
            }
            return data;
        }

        public void ChangeCurrentDirectory(string name)//method for changing the current directory by given name
        {
            if (name == "..")
            {
                _FileSystemMetadata.RootDirectoryId = Directories[_FileSystemMetadata.RootDirectoryId].ParentId;
            }
            else
            {
                int directoryId = FindDirectory(name, _FileSystemMetadata.RootDirectoryId);
                if (directoryId == -1)
                {
                    Console.WriteLine("Directory not found.");
                }
                _FileSystemMetadata.RootDirectoryId = directoryId;
            }
        }
        public void Write(string fileName, string content, bool append = false)//method for writing content in a file, if append is false=>new file is created(if it exists it writes over it)
                                                                               //if append is true the content is added after the current content in the existing file
        {
            int fileId = FindFile(fileName, _FileSystemMetadata.RootDirectoryId);
            if (fileId == -1)
            {
                CreateFile(fileName, _FileSystemMetadata.RootDirectoryId);
                fileId = FindFile(fileName, _FileSystemMetadata.RootDirectoryId);
            }
            WriteToFile(fileId, Encoding.UTF8.GetBytes(content), append);
        }

        private void CreateFile(string fileName, int directoryId)//Create file with given name and root directory id
        {
            var file = new File(nextId++, fileName);
            Files[file.Id] = file;
            Directories[directoryId].Children.Add(file.Id);
            _FileSystemMetadata.FileCount++;
            Save();
        }

        private void WriteToFile(int fileId, byte[] bytes, bool append)
        {
            throw new NotImplementedException();
        }
        public void DeleteFile(string fileName) //method for deleting a file with a given name
        {
            var parent = Directories[_FileSystemMetadata.RootDirectoryId];
            var fileToDeleteId = FindFile(fileName, parent.Id);
            if (fileToDeleteId == -1)
            {
                Console.WriteLine("No such file exists.");
                return;
            }
            var file = Files[fileToDeleteId];
            if (Files.ContainsKey(fileToDeleteId))
            {
                foreach (var block in file.Blocks )
                {
                    SetBlock(block, true);
                    _FileSystemMetadata.FreeBlocks++;

                }
            }
            Files.Remove(fileToDeleteId);
            Directories[parent.Id].Children.Remove(fileToDeleteId);
            _FileSystemMetadata.FileCount--;
            Save();
        }

        private void SetBlock(int blockIndex, bool free) //method for updating a certain block in the bitmap
        {
            int byteIndex = blockIndex / 8;
            int bitIndex = byteIndex % 8;
            byte mask = 1;
            for (int i = 0; i < bitIndex; i++)
            {
                mask *= 2;
            }
            if (free)
            {
                Bitmap[byteIndex] += mask;
            }
            else
            {
                if ((Bitmap[byteIndex] & mask) != 0) 
                {
                    Bitmap[byteIndex] -= mask; 
                }
            }
        }
    }
}
