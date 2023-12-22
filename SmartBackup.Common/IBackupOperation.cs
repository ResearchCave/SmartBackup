using SmartBackup.Model;
using System;
using System.Threading.Tasks;
using static SmartBackup.Common.BackupOperation;

namespace SmartBackup.Common
{
    public interface IBackupOperation
    {
        void PreConfigure(IServiceProvider _sp);
        void PostConfigure();
            //BackupInfo Info { get; set; }
        event EventHandler<UpdateProgressEventArgs> UpdateProgress;
        void ReportProgress(float progress);
        bool EncryptionSupported { get;  } 
        void Configure(object  o);
            Task BackupAsync();
    }
}
