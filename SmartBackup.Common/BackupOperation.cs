using ConsoleApp38;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SmartBackup.Model;
using System.IO;
using SmartBackup.Services;


namespace SmartBackup.Common
{
    public abstract class BackupOperation : IBackupOperation
    {
        private IServiceProvider sp;
        protected readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public abstract bool EncryptionSupported { get; }
        public virtual bool TestPath()
        {
            return true;
        }
        public class UpdateProgressEventArgs : EventArgs
        {
            public UpdateProgressEventArgs(float progress)
            {
                this.Progress = progress;
            }
            public float Progress { get; set; }
        }

        public event EventHandler<UpdateProgressEventArgs> UpdateProgress;

        public void ReportProgress(float f)
        {
            if (UpdateProgress != null)
                UpdateProgress(this, new UpdateProgressEventArgs(f));
        }

        public abstract string GetBackupFileName();

        public  abstract Task BackupAsync();

        protected IBackupInfo Data { get; set; }
        public void PreConfigure(IServiceProvider _sp)
        {
            sp = _sp;
        }
        public void PostConfigure()
        {
            IConfReader cr = (IConfReader)sp.GetService(typeof(IConfReader));
            if (Data.BackupPath == null)
            {
                Data.BackupPath = cr.Configuration?.BackupPath;
            }
            if (!Directory.Exists(Data.BackupPath))
            {

                Directory.CreateDirectory(Data.BackupPath);
            }
            ProcessManager pm = (ProcessManager)sp.GetService(typeof(ProcessManager));

            if (Data.CloseProcesses != null)
                foreach (string pname in Data.CloseProcesses)
                {
                    string procname = pname;
                    pm.CloseProcesses(procname);
                }
        }
        public abstract void Configure(object o);


        static readonly char[] turChars = { ' ', '_', 'Ğ', 'ğ', 'Ü', 'ü', 'Ş', 'ş', 'İ', 'ı', 'Ö', 'ö', 'Ç', 'ç' };
        static readonly char[] engChars = { ' ', '_', 'G', 'g', 'U', 'u', 'S', 's', 'I', 'i', 'O', 'o', 'C', 'c' };


        //*1000 => 2ms
        public static string GenerateSlug(string str, int maxlen = 50)
        {

            StringBuilder sb = new StringBuilder();
            bool wasHyphen = true;
            int MaxCnt = str.Length > maxlen ? maxlen : str.Length;

            //  foreach (char c in str)
            for (int i = 0; i < MaxCnt; i++)
            {
                char c = str[i];
                bool wastr = false;

                //  if (char.IsLetterOrDigit(c))
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-')
                {
                    sb.Append(c);
                    wasHyphen = false;
                }
                else if (char.IsWhiteSpace(c) && !wasHyphen)
                {
                    sb.Append(' ');
                    wasHyphen = true;
                }
                else
                {
                    for (int j = 0; j < turChars.Length; j++)
                    {

                        if (c == turChars[j])
                        {
                            sb.Append(engChars[j]);
                        }
                        wastr = true;
                        wasHyphen = false;
                        //  text = text.Replace(olds[i], news[i]);
                    }
                    if (!wastr) sb.Append("-");
                }

            }
            // Avoid trailing hyphens
            if (wasHyphen && sb.Length > 0)
                sb.Length--;
            str = sb.ToString();


            return str.ToLowerInvariant();

            //return str.Substring(0, str.Length <= 45 ? str.Length : 45);
        }

        public static string Asciify(string str)
        {
            // char[] TrChars = new char[] { 'Ş', 'İ', 'ı', 'Ğ', 'ğ', 'Ü', 'ü', 'ç', 'Ç', 'ö', 'Ö', 'ş', 'â', 'ê', 'î', 'û', 'Â', 'Ê', 'Î', 'Û', 'ä', 'Ä', 'ß' };
            // char[] EnChars = new char[] { 's', 'i', 'i', 'g', 'g', 'u', 'u', 'c', 'c', 'o', 'o', 's', 'a', 'e', 'i', 'u', 'a', 'e', 'i', 'u', 'a', 'a', 's' };

            string TrChars = "ŞİıĞğÜüçÇöÖşâêîûÂÊÎÛäÄß";
            string EnChars = "siigguuccoosaeiuaeiuaas";

            if (str == null) return null;

            char[] cc = str.ToCharArray();
            for (int i = 0; i < cc.Length; i++)
            {
                int ind = TrChars.IndexOf(cc[i]);

                if (ind >= 0)
                {
                    cc[i] = EnChars[ind];
                }
            }

            string str2 = new string(cc).ToLowerInvariant();
            return str2;
        }

   
    }
}
