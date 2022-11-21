 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartBackup.Model
{
    public enum ErrorActionPreferences
    {
        Stop, Warn
    }
    public class Config  
    {
        [Required]

        public ErrorActionPreferences ErrorActionPreference { get; set; } = ErrorActionPreferences.Stop;

        [DisplayName("Backup Path")]
        [Description("The target folder to store all backedup files. This is the default destination path for all items if they do not have BackupPath property explicitly.")]
        public string BackupPath { get; set; }

        [DisplayName("Temporary Storage Path")]
        [Description("This path is used if current backup job requires a temporary location to save some files. (If it needs to store pre-encryption or if a network mount cannot be available). This is default temp folder if not specified")]

        public string TempPath { get; set; }

        [DisplayName("SMTP Settings")]
        [Description("SMTP Settings to get backup reports if requested")]


         public  SMTPSettings SMTP { get; set; }

        [Required]
        [NotNull]
        public IEnumerable<object>  Items  { get; set; } 

        [DisplayName("Compression Level")]
        public int CompressionLevel { get; set; }
        [Description("Default password for archives, this password will be used for all backup modules supporting encryption")]
        public string Password { get; set; }

        [DefaultValue(true)]
        [Description("Send mail after all backup jobs (Using SMTP Settings). If this property is not specified, mail report will be sent if SMTP settings are present.")]
        public bool SendReport { get; set; } = true;

    }
}
