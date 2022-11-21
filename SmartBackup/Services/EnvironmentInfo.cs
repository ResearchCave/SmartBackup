using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Services
{
    public class EnvironmentInfo
    {
        public DateTime StartTime  { get; set; }    
        public string AppPath { get; set; }
        public string CurrentLogPath { get; set; }
       public string MachineName { get { return System.Environment.MachineName; } }
    }
}
