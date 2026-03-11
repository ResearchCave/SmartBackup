using SmartBackup.Common;
using SmartBackup.Model;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SmartBackup.Redis
{
    [BackupModule(Type = "Redis", ConfigurationType = typeof(RedisInfo))]
    public class RedisBackupOperation : BackupOperation
    {
        private readonly ProcessManager pm;

        public RedisBackupOperation(ProcessManager _pm)
        {
            pm = _pm;
        }

        public RedisInfo Info { get { return (RedisInfo)Data; } set { Data = value; } }

        public override bool EncryptionSupported { get { return false; } }

        public override void Configure(object o)
        {
            JsonElement jitem = (JsonElement)o;
            Info = jitem.Deserialize<RedisInfo>(SerializationOptions);
        }

        public override async Task BackupAsync()
        {
            string backupFile = Path.Combine(Info.BackupPath, GetBackupFileName());

            string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "redis-cli.exe" : "redis-cli";
            string exePath = string.IsNullOrWhiteSpace(Info.RedisCliPath)
                ? exeName
                : Path.Combine(Info.RedisCliPath, exeName);

            string host = string.IsNullOrWhiteSpace(Info.Host) ? "localhost" : Info.Host;
            int port = Info.Port ?? 6379;
            int db = Info.Database ?? 0;

            List<string> args = new List<string>();

            args.Add("--rdb");
            args.Add(backupFile);

            args.Add("-h");
            args.Add(host);

            args.Add("-p");
            args.Add(port.ToString());

            if (db > 0)
            {
                args.Add("-n");
                args.Add(db.ToString());
            }

            if (!string.IsNullOrWhiteSpace(Info.Username))
            {
                args.Add("--user");
                args.Add(Info.Username);
            }

            if (!string.IsNullOrWhiteSpace(Info.Password))
            {
                args.Add("-a");
                args.Add(Info.Password);
            }

            ReportProgress(0);
            int result = pm.StreamCommand(exePath, QuoteArgs(args));
            if (result != 0)
            {
                throw new Exception(string.Format("Unexpected return code {0}, Backup job failed.", result));
            }
            ReportProgress(100);
        }

        public override string GetBackupFileName()
        {
            int db = Info.Database ?? 0;
            return $"redis-db{db}.rdb";
        }

        private static string[] QuoteArgs(List<string> args)
        {
            string[] quoted = new string[args.Count];
            for (int i = 0; i < args.Count; i++)
            {
                quoted[i] = QuoteIfNeeded(args[i]);
            }
            return quoted;
        }

        private static string QuoteIfNeeded(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Contains(' ') || value.Contains('\t') || value.Contains('"'))
            {
                return "\"" + value.Replace("\"", "\\\"") + "\"";
            }

            return value;
        }
    }
}
