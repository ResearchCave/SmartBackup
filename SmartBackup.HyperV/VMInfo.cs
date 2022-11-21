using SmartBackup.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.HyperV
{

 
    public  class VMInfo: BackupInfo
	{
        [Required]
        public string Server { get; set; }    
        [Description("Destination Folder; Example: D:\\Backups\\")]
        public new string? BackupPath { get { return base.BackupPath; } set { base.BackupPath = value; } }
        [Required]
        [Description("Name of Virtual Machine")]
        public string VM { get; set; }
        [Description("Username for HyperV authentication. Set null for windows authentication")]
        public string? Username { get; set; }
        public string? Password { get; set; }

        [DefaultValue(true)]
        [DisplayName("Overwrite existing vm backup if exists")]
        [Description("HyperV Export does not overwrite existing backup by default, If this flag is true, existing backup will be removed prior to backup operation")]

        public bool OverwriteBackupIfExists { get; set; } = true;

        [DisplayName("Temp Path")]
        [Description("HyperV uses its own user which may not have access to shared folders. If this is the case, you can use a temporary local folder and it will be copied from the temp path to your target folder after the backup operation")]

        public string TempPath { get; set; }


    }
}
