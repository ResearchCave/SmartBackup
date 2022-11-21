using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Model
{
     
    public abstract class BackupInfo:IBackupInfo
	{
        [NotNull]
        [Required]
        [MinLength(1)]
        public string Type { get;  }
        [Description("Name of this job, must be unique")]
        public string  Name {  get; set; }

        [Description("These processes will be closed before backup operation, type only process names in the items (Example: calc.exe)")]
        public IEnumerable<string> CloseProcesses { get; set; }

        [Description("Destination Folder; Example: D:\\Backups\\\r\nChange this if you want to back up to any other location than global backup location")]
        public string? BackupPath { get; set; }
    }  
}
