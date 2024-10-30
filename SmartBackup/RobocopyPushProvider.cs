using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Common {
    public class RobocopyPushProvider : IPushProvider {
        public string Source { get; set; }
        public string Dest { get; set; }

        public void Push() {
        //   RoboSharp.RoboCommand rc= new RoboSharp.RoboCommand();
   
        }
    }
}
