using System;
using System.Collections.Generic; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Model
{
    public interface IBackupInfo {
    //    string Name { get; }
        string Type { get; }
        string? BackupPath { get; set; }
        IEnumerable<string> CloseProcesses { get; set; }
    }  
}
