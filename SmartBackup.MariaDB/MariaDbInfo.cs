using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartBackup.Model
{
    public class MariaDbInfo : BackupInfo
    {
        [Required]
        [DefaultValue("localhost")]
        public string Host { get; set; }

        [DefaultValue(3306)]
        public int? Port { get; set; }

        [Required]
        [Description("Name of MariaDB Database")]
        public string Database { get; set; }

        public string? Username { get; set; }
        public string? Password { get; set; }

        [Description("Local path containing MariaDB binaries (mariadb-dump). Optional if mariadb-dump is on PATH.")]
        public string? MariaDbBinDir { get; set; }

        [Description("Destination Folder; Example: D:\\Backups\\\r\nChange this if you want to back up to any other location than global backup location")]
        public new string? BackupPath { get { return base.BackupPath; } set { base.BackupPath = value; } }
    }
}
