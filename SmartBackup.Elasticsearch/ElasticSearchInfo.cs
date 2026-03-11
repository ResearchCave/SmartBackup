using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartBackup.Model
{
    public class ElasticSearchInfo : BackupInfo
    {
        [Required]
        [DefaultValue("localhost")]
        public string Host { get; set; }

        [DefaultValue(9200)]
        public int? Port { get; set; }

        [DefaultValue("http")]
        public string? Scheme { get; set; }

        [Required]
        [Description("Snapshot repository name configured in Elasticsearch.")]
        public string SnapshotRepository { get; set; }

        [Description("Snapshot name. If omitted, SmartBackup generates a timestamped name.")]
        public string? SnapshotName { get; set; }

        [Description("Optional index names to include. Leave empty to snapshot all indices.")]
        public IEnumerable<string>? Indices { get; set; }

        [Description("Optional base path prefix if Elasticsearch is behind a reverse proxy, e.g. /es.")]
        public string? PathPrefix { get; set; }

        [Description("Basic authentication username.")]
        public string? Username { get; set; }

        [Description("Basic authentication password.")]
        public string? Password { get; set; }

        [Description("ApiKey authentication value (base64 encoded id:api_key).")]
        public string? ApiKey { get; set; }

        [DefaultValue(false)]
        [Description("Set to true to skip TLS certificate validation.")]
        public bool? IgnoreCertificateErrors { get; set; }

        [DefaultValue(false)]
        [Description("Whether to include cluster global state in snapshot.")]
        public bool? IncludeGlobalState { get; set; }

        [Description("Destination Folder; Example: D:\\Backups\\\r\nChange this if you want to back up to any other location than global backup location")]
        public new string? BackupPath { get { return base.BackupPath; } set { base.BackupPath = value; } }
    }
}
