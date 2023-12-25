using SmartBackup.Common;
using SmartBackup.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartBackup.Archiver
{

    [BackupModule( Type="File", ConfigurationType = typeof(Archiver.ArchiveInfo))]
    public class  ArchiveOperation : BackupOperation
	{
        readonly ProcessManager pm;
        public ArchiveOperation(ProcessManager _pm) {
            pm = _pm;
        }

        public  ArchiveInfo Info { get { return (ArchiveInfo)Data; }  set { Data = value; } } 

        public override bool EncryptionSupported { get { return true; } }
         
      

        public string  zpaqexe { get {

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (RuntimeInformation.OSArchitecture == Architecture.X64)
                        return "zpaq64.exe";
                    else if (RuntimeInformation.OSArchitecture == Architecture.X86)
                        return "zpaq.exe";
                    else return null;
                }

                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "zpaq.mac"; 
                }

                else return null;
			} 
        }
        public override bool TestPath()
        {

            if(!File.Exists(zpaqexe))
            {
                return false;
            }

             bool exits= Directory.Exists(Info.Path);
            if(!exits) exits=File.Exists(Info.Path);

            return exits;
        }


        public override string GetBackupFileName()
        {
            const string ext = ".zpaq";
            string FnFromPath = BackupOperation.GenerateSlug( BackupOperation.Asciify( System.IO.Path.GetFileName( Info.Path.TrimEnd(new char[] { '/', '\\'}))));
             
            if(String.IsNullOrEmpty(FnFromPath) || (FnFromPath??"").Length==1)
            {
                if (!String.IsNullOrEmpty(Info.Name))
                {
                    FnFromPath = BackupOperation.GenerateSlug(BackupOperation.Asciify(Info.Name));
                }
            }

            return FnFromPath + ext;
        }

        private    void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8192];

            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
 

        public override void Configure(object o)
        {
            JsonElement jitem=(JsonElement)o;

            if (jitem.ValueKind == JsonValueKind.String)
            {
                Info.Path = jitem.GetString();
            }
            else if (jitem.ValueKind == JsonValueKind.Object)
            {
                Info =  jitem.Deserialize<ArchiveInfo>();
            }
            else
            {
                throw new Exception( String.Format("Unexpected Item Type:{0}" , jitem.ValueKind ));
            }

        }

        public override async  Task  BackupAsync() {
            string zipname = GetBackupFileName();
            string BackupFullFileName = Path.Combine(Info.BackupPath, zipname);
            string zpaqfullpath = "";

			 
				zpaqfullpath = zpaqexe; 
		 


			if (!System.IO.File.Exists(zpaqexe)) {
                string SmartBackupTempPath = Path.Combine(Path.GetTempPath(), "SmartBackup");
                if (!Directory.Exists(SmartBackupTempPath)) Directory.CreateDirectory(SmartBackupTempPath);

                string newzpaqpath = Path.Combine(SmartBackupTempPath, zpaqexe);

                var assembly = Assembly.GetExecutingAssembly();
                //    var resourceNames = assembly.GetManifestResourceNames();
                var resourceName = "SmartBackup.Archiver." + zpaqexe;


                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (Stream output = File.Create(newzpaqpath)) {
                    CopyStream(stream, output);
                }
                zpaqfullpath = newzpaqpath;
            }

            List<string> args = new List<string>();

            //$cmdtorun = """zpaq.exe"" add    ""$backupdir$zipname"" ""$Item"""
            args.Add("add");
            args.Add(BackupFullFileName);
            args.Add(Info.Path);

            if (Info.Skip != null)
                foreach (string skip in Info.Skip) {
                    args.Add("-not");
                    args.Add(skip);
                }

            if (!String.IsNullOrEmpty(this.Info.Password)) {
                args.Add("-key");
                args.Add(this.Info.Password);
            }

            ReportProgress(0);

            int result = pm.StreamCommand(zpaqfullpath, args.ToArray());

            if (result != 0) {
                throw new Exception(String.Format("Unexpected return code {0}, Backup job failed.", result));
            }

            ReportProgress(100);

          
        }
    }
}
