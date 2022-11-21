using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Model
{
    public  class MySQLInfo: BackupInfo
	{
        [Required]
        [DefaultValue("localhost")]
        public string Server { get; set; }
        [Description("Destination Folder; Example: D:\\Backups\\ \r\nNote: This should be a folder on the PC where MySql service is installed. (or a remote disk which is accessible by the account SQLServer is running on)")]
         public new string? BackupPath { get { return base.BackupPath; } set { base.BackupPath = value; } }
        [Required]
        [Description("Name of MySQL Database")]
        public string Database { get; set; }

        [Description("Username for Mysql authentication.")]
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
