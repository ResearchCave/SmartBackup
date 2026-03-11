using SmartBackup.Common;
using SmartBackup.Model;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SmartBackup.MariaDB
{
    [BackupModule(Type = "MariaDB", ConfigurationType = typeof(MariaDbInfo))]
    public class MariaDbBackupOperation : BackupOperation
    {
        private readonly ProcessManager pm;

        public MariaDbBackupOperation(ProcessManager _pm)
        {
            pm = _pm;
        }

        public MariaDbInfo Info { get { return (MariaDbInfo)Data; } set { Data = value; } }

        public override bool EncryptionSupported { get { return false; } }

        public override void Configure(object o)
        {
            JsonElement jitem = (JsonElement)o;
            Info = jitem.Deserialize<MariaDbInfo>(SerializationOptions);
        }

        public override async Task BackupAsync()
        {
            string backupFile = Path.Combine(Info.BackupPath, GetBackupFileName());

            string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mariadb-dump.exe" : "mariadb-dump";
            string exePath = string.IsNullOrWhiteSpace(Info.MariaDbBinDir)
                ? exeName
                : Path.Combine(Info.MariaDbBinDir, exeName);

            string host = string.IsNullOrWhiteSpace(Info.Host) ? "localhost" : Info.Host;
            int port = Info.Port ?? 3306;

            List<string> args = new List<string>();

            args.Add("--host");
            args.Add(host);
            args.Add("--port");
            args.Add(port.ToString());
            args.Add("--result-file");
            args.Add(backupFile);

            if (!string.IsNullOrWhiteSpace(Info.Username))
            {
                args.Add("--user");
                args.Add(Info.Username);
            }

            if (!string.IsNullOrWhiteSpace(Info.Password))
            {
                args.Add("--password=" + Info.Password);
            }
            else
            {
                args.Add("--skip-password");
            }

            args.Add(Info.Database);

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
            return Info.Database + ".sql";
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
