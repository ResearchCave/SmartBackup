using SmartBackup.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Archiver
{
	public class ArchiveInfo : BackupInfo 
	{
		[Required]
		[NotNull]
		[Description("Path to backup; Example: C:\\Projects\\")]
		public string Path { get; set; } = "";

        [Range(0, 255)]
        [Description("Compression Level \r\n\r\n0:Uncompressed\r\n 1:Low compression \r\n2:MediumCompression \r\n3: High Comression ")]
		[DefaultValue(2)]
        public int? CompressionLevel { get; set; }
        [Description("Password for this backup")]
        public string? Password { get; set; }

        [Description("Files/Folders will be skipped if they contain this string in their filename\r\nIn the first form, do not add, extract, or list files that match any file by name.\r\n\r\nfile may contain wildcards * and ? that match any string or character respectively, including /. A match to a directory also matches all of its contents. In Windows, matches are not case sensitive, and \\ matches /.\r\n\r\nIn Unix/Linux, arguments with wildcards must be quoted to protect them from the shell.")]
        public IEnumerable<string>? Skip { get; set; } 


	}
}
