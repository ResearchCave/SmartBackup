using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Common {
    public  interface IPushProvider {

      
        public string Source { get; set; }
        public string Dest { get; set; }
        void Push();
    }
}
