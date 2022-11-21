
using MySql.Data.MySqlClient;
using MySQLBackupNetCore;
using SmartBackup.Common;
using SmartBackup.Model;
using System.Text.Json;

namespace SmartBackup.MSSQL
{
    [BackupModule(Type = "MySQL" ,   ConfigurationType = typeof(MySQLInfo))]
    public class MySQLBackupOperation :   BackupOperation
    {
        public MySQLBackupOperation()
        {

        }
        public MySQLInfo Info { get { return (MySQLInfo)Data; } set { Data = value; } }

        public override bool EncryptionSupported { get { return false ; } }
        public override void Configure(object o)
        {
           JsonElement jitem = (JsonElement)o;
           Info = jitem.Deserialize<MySQLInfo>(SerializationOptions);
        }
		public override void Backup()
        {
            string BackupName = Info.Database + ".sql";
            string BackupFile = Path.Combine(Info.BackupPath, BackupName);

            string constring = $"server={Info.Server};user={Info.Username};pwd={Info.Password};";
            if (!String.IsNullOrEmpty(Info.Database))
            {
                constring += $"database={Info.Database};";
            }

            // Important Additional Connection Options
            constring += "charset=utf8;convertzerodatetime=true;";

            ReportProgress(0);

            using (MySqlConnection conn = new MySqlConnection(constring))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        mb.ExportProgressChanged += Mb_ExportProgressChanged;
                        mb.ExportCompleted += Mb_ExportCompleted; ;

                        cmd.Connection = conn;
                        conn.Open();
                        mb.ExportToFile(BackupFile);
                        conn.Close();
                    }
                }
            }
            ReportProgress(100);

        }

        private void Mb_ExportCompleted(object sender, MySQLBackupNetCore.EventArgs.ExportCompleteArgs e)
        {
            ReportProgress(100);
        }

        private void Mb_ExportProgressChanged(object sender, MySQLBackupNetCore.EventArgs.ExportProgressArgs e)
        {
            ReportProgress((float)(e.CurrentRowIndexInAllTables  *100 / e.TotalRowsInAllTables));
        }

        public override string GetBackupFileName()
        {
            throw new NotImplementedException();
        }
    }
}