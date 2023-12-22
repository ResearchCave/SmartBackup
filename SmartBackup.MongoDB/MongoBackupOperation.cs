 
using SmartBackup.Common;
using SmartBackup.Model;
using System.Text.Json;

namespace SmartBackup.MongoDb
{
    [BackupModule(Type = "MongoDB" ,   ConfigurationType = typeof(MongoInfo))]
    public class MongoBackupOperation :   BackupOperation
    {
        public readonly ProcessManager pm;

        public MongoBackupOperation(ProcessManager _pm)
        {
            pm = _pm;
        }
        public MongoInfo Info { get { return (MongoInfo)Data; } set { Data = value; } }
        public override bool EncryptionSupported { get { return false ; } }

        public override void Configure(object o)
        {
            JsonElement jitem = (JsonElement)o;
            Info = jitem.Deserialize<MongoInfo>(SerializationOptions);
        }

        const int MongoDBDefaultPort = 27019;

        public override async  Task BackupAsync()
        {
            string mongodumpexe = "mongodump.exe";
            //mongodump --port 27020 --db test --out /mydata/restoredata/
            string zipname = GetBackupFileName();
            string BackupFullFileName = Path.Combine(Info.BackupPath, zipname);

            int port = MongoDBDefaultPort;
            if(Info.Port.HasValue)
            {
                port = Info.Port.Value;
                if (port == 0) port = MongoDBDefaultPort;
            }

            List<string> args = new List<string>();
 
            if(!String.IsNullOrEmpty(Info.Host))
            { 
            args.Add("--host");
            args.Add(Info.Host);
            }

            if(!String.IsNullOrEmpty(Info.AuthenticationDatabase))
            {
                args.Add("--authenticationDatabase");
                args.Add(Info.AuthenticationDatabase);
            }
            else
            {
                args.Add("--authenticationDatabase");
                args.Add("admin");
            }
            args.Add("--port");
            args.Add(port.ToString());
            args.Add("--db");
            args.Add(Info.Database);
            args.Add("--out");
            args.Add(Info.BackupPath);

            if (!String.IsNullOrEmpty(Info.Username))
            {
                args.Add("--username");
                args.Add(Info.Username);
            }
            if (!String.IsNullOrEmpty(Info.Password))
            {
                args.Add("--password");
                args.Add(Info.Password);
            }
            ReportProgress(0);

            string mongodumpFullPath = Path.Combine(Info.MongoDbDir, mongodumpexe);
            string dbg = mongodumpFullPath + " " + string.Join(" ", args);
          //      Console.WriteLine(dbg);
            int result = pm.StreamCommand(mongodumpFullPath, args.ToArray());
            if (result != 0)
            {
                throw new Exception(String.Format("Unexpected return code {0}, Backup job failed.", result));
            }
            ReportProgress(100);
        }

        public override string GetBackupFileName()
        {
            return Info.Database;
        }
    }
}