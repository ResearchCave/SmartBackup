using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartBackup.Model
{
    public class RedisInfo : BackupInfo
    {
        [Required]
        [DefaultValue("localhost")]
        public string Host { get; set; }

        [DefaultValue(6379)]
        public int? Port { get; set; }

        [DefaultValue(0)]
        public int? Database { get; set; }

        public string? Username { get; set; }
        public string? Password { get; set; }

        [Description("Local path to redis-cli executable. Optional if redis-cli is on PATH.")]
        public string? RedisCliPath { get; set; }

        [Description("Destination Folder; Example: D:\\Backups\\\r\nChange this if you want to back up to any other location than global backup location")]
        public new string? BackupPath { get { return base.BackupPath; } set { base.BackupPath = value; } }
    }
}
