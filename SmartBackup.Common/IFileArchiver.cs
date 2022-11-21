using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Archiver
{
    public interface IFileArchiver
    {
        public string Path { get; set; }
        public string ArchivePath { get; set; }
        public CompressionLevel CompressionLevel { get; set; }

        public void Compress();

 


    }
}
