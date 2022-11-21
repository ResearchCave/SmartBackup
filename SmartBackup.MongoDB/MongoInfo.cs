using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Model
{

 
    public  class MongoInfo: BackupInfo
	{
        [Required]
        [Description("Local path containing MongoDb binaries.")]
        public string MongoDbDir { get; set; }
        public int? Port { get; set; }

        [Required]
        public string Database { get; set; }
        [Required]
        [Description("Mongo server hostname (without port).")]
        [DefaultValue("localhost")]
        public string Host { get;   set; }
        [DefaultValue("admin")]
        public string? AuthenticationDatabase { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
