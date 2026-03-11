using SmartBackup.Common;
using SmartBackup.Model;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SmartBackup.PostgreSQL
{
    [BackupModule(Type = "PostgreSQL", ConfigurationType = typeof(PostgreSqlInfo))]
    public class PostgreSqlBackupOperation : BackupOperation
    {
        private readonly ProcessManager pm;

        public PostgreSqlBackupOperation(ProcessManager _pm)
        {
            pm = _pm;
        }

        public PostgreSqlInfo Info { get { return (PostgreSqlInfo)Data; } set { Data = value; } }

        public override bool EncryptionSupported { get { return false; } }

        public override void Configure(object o)
        {
            JsonElement jitem = (JsonElement)o;
            Info = jitem.Deserialize<PostgreSqlInfo>(SerializationOptions);
        }

        public override async Task BackupAsync()
        {
            string backupFile = Path.Combine(Info.BackupPath, GetBackupFileName());

            string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pg_dump.exe" : "pg_dump";
            string exePath = string.IsNullOrWhiteSpace(Info.PostgreSqlBinDir)
                ? exeName
                : Path.Combine(Info.PostgreSqlBinDir, exeName);

            string host = string.IsNullOrWhiteSpace(Info.Host) ? "localhost" : Info.Host;
            int port = Info.Port ?? 5432;

            List<string> args = new List<string>();

            if (!string.IsNullOrWhiteSpace(Info.Username))
            {
                string user = Uri.EscapeDataString(Info.Username);
                string db = Uri.EscapeDataString(Info.Database);
                string passwordPart = "";

                if (!string.IsNullOrWhiteSpace(Info.Password))
                {
                    string pass = Uri.EscapeDataString(Info.Password);
                    passwordPart = $":{pass}";
                }

                string conn = $"postgresql://{user}{passwordPart}@{host}:{port}/{db}";
                args.Add($"--dbname={conn}");
            }
            else
            {
                args.Add("--dbname");
                args.Add(Info.Database);
            }

            args.Add("--host");
            args.Add(host);
            args.Add("--port");
            args.Add(port.ToString());
            args.Add("--format=custom");
            args.Add("--file");
            args.Add(backupFile);

            if (!string.IsNullOrWhiteSpace(Info.Username) && string.IsNullOrWhiteSpace(Info.Password))
            {
                args.Add("--no-password");
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
            return Info.Database + ".backup";
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
