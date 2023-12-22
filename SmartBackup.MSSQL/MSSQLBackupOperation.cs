using Microsoft.IdentityModel.Abstractions;
using Microsoft.SqlServer.Management.Smo;
using SmartBackup.Common;
using SmartBackup.Model;
using System.IO.Compression;
using System.Text.Json;
using Serilog;

namespace SmartBackup.MSSQL
{
    [BackupModule(Type = "MSSQL" ,   ConfigurationType = typeof(MSSQLInfo))]
    public class MSSQLBackupOperation :   BackupOperation
    {
        readonly ILogger log;
      //  readonly Archiver.IFileArchiver fa;
        public MSSQLBackupOperation(ILogger _log )
        {
           
            // fa  = _fa;
            this.log = _log;
        }
        public MSSQLInfo Info { get { return (MSSQLInfo)Data; } set { Data = value; } }
        public override bool EncryptionSupported { get { return false ; } }

        public override void Configure(object o)
        {
            JsonElement jitem = (JsonElement)o;
            Info = jitem.Deserialize<MSSQLInfo>(SerializationOptions);
            
        }
       
		public override async  Task BackupAsync()
        {

            Server myServer = null;
            Database myDatabase = null;
            string BackupName = Info.Database + ".bak";

            Microsoft.SqlServer.Management.Common.ServerConnection conn =  new Microsoft.SqlServer.Management.Common.ServerConnection();
            conn.ServerInstance = Info.Server;
            


            if ( Info.Username!= null ) {
                conn.LoginSecure = false;
                
                conn.Authentication = Microsoft.SqlServer.Management.Common.SqlConnectionInfo.AuthenticationMethod.SqlPassword;
                
                conn.Login = Info.Username;
                if(Info.Password!= null ) {
                    conn.Password = Info.Password;
                }
            }
            else
            {
                conn.LoginSecure = true;
            }



            string backupPath = Info.BackupPath;
            if (!String.IsNullOrEmpty(Info.TempPath))
            {
                string tmpPath = Info.TempPath;
                if(tmpPath.Contains("%temp%", StringComparison.OrdinalIgnoreCase)) { 
                tmpPath = tmpPath.Replace("%temp%", Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
                }
                if (!Directory.Exists(tmpPath)) Directory.CreateDirectory(tmpPath);

                backupPath = tmpPath;
            }

            /*
            BackupEncryptionOptions beo = new BackupEncryptionOptions();
            beo.Algorithm = BackupEncryptionAlgorithm.Aes256;
            beo.NoEncryption = false;
            beo.EncryptorType = BackupEncryptorType.ServerAsymmetricKey;
            */

            conn.DatabaseName = Info.Database; // You cannot restore a database that you are connected to
            myServer = new Server(conn);

            string BackupFile = Path.Combine(backupPath, BackupName);
            Backup bkpDBFull = new Backup();
            /* Specify whether you want to back up database or files or log */
            bkpDBFull.Action = BackupActionType.Database;
            /* Specify the name of the database to back up */
            bkpDBFull.Database = Info.Database;
            /* You can take backup on several media type (disk or tape), here I am
             * using File type and storing backup on the file system */
             bkpDBFull.SkipTapeHeader= true;
            bkpDBFull.FormatMedia = false ;
         
            bkpDBFull.CompressionOption = BackupCompressionOptions.On;
            bkpDBFull.Devices.AddDevice(BackupFile, Microsoft.SqlServer.Management.Smo.DeviceType.File);
            bkpDBFull.BackupSetName = $"{Info.Database} Database Backup";
          
          //  bkpDBFull.BackupSetDescription = "Pasaj database - Full Backup";
            /* You can specify the expiration date for your backup data
             * after that date backup data would not be relevant */
            //  bkpDBFull.ExpirationDate = DateTime.Today.AddDays(10);

            /* You can specify Initialize = false (default) to create a new 
             * backup set which will be appended as last backup set on the media. You
             * can specify Initialize = true to make the backup as first set on the
             * medium and to overwrite any other existing backup sets if the all the
             * backup sets have expired and specified backup set name matches with
             * the name on the medium */
            if (Info.BackupType== SQLBackupType.Full)
            {
                bkpDBFull.Initialize = true ;
            }
            else if (Info.BackupType == SQLBackupType.Incremental)
            {
                bkpDBFull.Initialize = false  ;
            }
           

            /* Wiring up events for progress monitoring */
            bkpDBFull.PercentComplete += BkpDBFull_PercentComplete; ;
            bkpDBFull.Complete += BkpDBFull_Complete;  ;
           
            /* SqlBackup method starts to take back up
             * You can also use SqlBackupAsync method to perform the backup 
             * operation asynchronously */

            bkpDBFull.SqlBackup(myServer);
           log.Information("Backup file saved in :{0}" , BackupFile);



            if (!String.IsNullOrEmpty(Info.TempPath))
            {
                string destFn = Path.Combine(Info.BackupPath, Path.GetFileName(BackupFile));

                System.IO.File.Copy(BackupFile, destFn, true);
              log.Information("Copied MSSQL Backup Temp Artifact to: {0}" , destFn);

                if(System.IO.File.Exists(BackupFile)) { 

                System.IO.File.Delete(BackupFile);
                   log.Information("Cleaning up...");
                }
                //string sqlbackupPath = Path.Combine(backupPath, "DB_"+ Info.Database);
                //string zipPath = Path.Combine(Info.BackupPath, "DB_" + Info.Database + ".zip");

                //if (File.Exists(zipPath)) System.IO.File.Delete(zipPath);

                //System.IO.Compression.ZipFile.CreateFromDirectory(sqlbackupPath, zipPath, CompressionLevel.Optimal, true);

                //try
                //{

                //    System.IO.Directory.Delete(sqlbackupPath, true);
                //}
                //catch (Exception x)
                //{

                //    System.Diagnostics.Trace.WriteLine("Could not delete temp VM backup folder:" + x.Message);
                //}
            }


        }

        private void BkpDBFull_Complete(object sender, Microsoft.SqlServer.Management.Common.ServerMessageEventArgs e)
        {
            Backup b = sender as Backup;
            ReportProgress(100);
        }

        private void BkpDBFull_PercentComplete(object sender, PercentCompleteEventArgs e)
        {
            ReportProgress(e.Percent);
        }

        public override string GetBackupFileName()
        {
            return Info.Name;
        //    throw new NotImplementedException();
        }
    }
}