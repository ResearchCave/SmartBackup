using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Common {
    public  interface IPushProvider {

     //   public string Name { get; set; }

          string Source { get; set; }
          string Dest { get; set; }
      void Connect();
        void Push();

        void DisConnect();
    }

    public abstract class PushProvider : IPushProvider
    {
        public abstract  string Source { get; set; }
        public abstract  string Dest { get; set; }

        public virtual void Connect()
        {
          //  throw new NotImplementedException();
        }

        public  virtual  void DisConnect()
        {
          //  throw new NotImplementedException();
        }

        public  abstract void Push();
    }
}
