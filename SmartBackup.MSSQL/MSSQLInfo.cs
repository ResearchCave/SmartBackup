using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Model
{

    public enum SQLBackupType
    {
        Full, Incremental
    }
    public  class MSSQLInfo: BackupInfo
	{
        [Required]
        public string Server { get; set; }

        [Description("Destination Folder; Example: D:\\Backups\\ \r\nNote: This should be a folder on the PC where SQL Server service is installed. (or a remote disk which is accessible by the account SQLServer is running on)")]
        public  new  string? BackupPath { get { return base.BackupPath; }    set { base.BackupPath = value; }  }

        [Required]
        [Description("Name of MSSQL Database")]
        public string Database { get; set; }
      

        [Description("Username for MSSQL authentication")]
        public string? Username { get; set; }
       

        public string? Password { get; set; }
        [DisplayName("Backup type")]
        [DefaultValue(SQLBackupType.Full )]
        public SQLBackupType BackupType { get; set; } = SQLBackupType.Full;
        [DisplayName("Temp Path")]
        [Description("MSSQL uses its own user which may not have access to shared folders. If this is the case, you can use a temporary local folder and it will be copied from the temp path to your target folder after the backup operation")]
        public string TempPath { get; set; }

    }
}
