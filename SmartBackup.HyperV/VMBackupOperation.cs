

using SmartBackup.Common;
using SmartBackup.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace SmartBackup.HyperV
{
    [BackupModule(Type = "HyperV" ,   ConfigurationType = typeof(VMInfo))]
    public class VMBackupOperation :   BackupOperation
    {
        readonly ProcessManager pm;
        public VMBackupOperation(ProcessManager _pm )
        {
            pm =_pm;
        }
        public VMInfo Info { get { return (VMInfo)base.Data; } set { base.Data = value; } }

        public override bool EncryptionSupported { get { return false ; } }

        public override void Configure(object o)
        {
            JsonElement jitem = (JsonElement)o;
            Info = jitem.Deserialize<VMInfo>(SerializationOptions);
           
        }
		public override void Backup()
        {
            string exportcmd = "Export-VM";
            string zipname = GetBackupFileName();
            string BackupFullFileName = Path.Combine(Info.BackupPath, zipname);

            string backupPath = Info.BackupPath;
            if(!String.IsNullOrEmpty(Info.TempPath))
            {
                string tmpPath = Info.TempPath;
                tmpPath = tmpPath.Replace("%temp%", Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);
                if (!Directory.Exists(tmpPath)) Directory.CreateDirectory(tmpPath);

                backupPath = tmpPath;
            }

            List<string> args = new List<string>();

            if( String.IsNullOrEmpty(Info.VM))
            {
                args.Add("Get-VM");
                args.Add("|"); 
            }

            args.Add(exportcmd);

            if(!String.IsNullOrEmpty(Info.VM))
            {
                args.Add("-Name");
                args.Add(Info.VM);
            }
  
            args.Add("-Path");
            args.Add(backupPath);

            ReportProgress(0);

            string dbg = "powershell.exe " +  exportcmd + " " + string.Join(" ", args);

            //      Console.WriteLine(dbg);

            if(Info.OverwriteBackupIfExists)
            {
              string prebackupPath = Path.Combine(backupPath, Info.VM);
                if(Directory.Exists(prebackupPath))
                {
                    Console.WriteLine("Deleting previous backup: " + prebackupPath);
                    Directory.Delete(prebackupPath, true);
                }
            }

            int result = pm.StreamCommand("powershell.exe", args.ToArray());

            if (result != 0)
            {
                throw new Exception(String.Format("Unexpected return code {0}, Backup job failed.", result));
            }
            ReportProgress(100);
            if (!String.IsNullOrEmpty(Info.TempPath))
            {
                string vmbackupPath = Path.Combine(backupPath, Info.VM);
                string zipPath = Path.Combine(Info.BackupPath, Info.VM + ".zip");

                if(File.Exists(zipPath)) System.IO.File.Delete(zipPath);

                System.IO.Compression.ZipFile.CreateFromDirectory(vmbackupPath, zipPath, CompressionLevel.Optimal ,true);
             
                try
                {

                    System.IO.Directory.Delete(vmbackupPath, true);
                }
                catch (Exception x)
                {

                    System.Diagnostics.Trace.WriteLine("Could not delete temp VM backup folder:"+x.Message);
                }
            }

        }
        public override string GetBackupFileName()
        {
            return "VM-"+(Info.VM??Info.Name);
        }
    }
}