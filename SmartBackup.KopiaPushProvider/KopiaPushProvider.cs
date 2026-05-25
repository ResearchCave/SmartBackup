using SmartBackup.Common;

namespace SmartBackup.KopiaPushProvider
{
    public class KopiaPushProvider : PushProvider
    {
        public required override string Source { get; set; }
         public required override string Dest { get; set; }
 
        public override  void Connect()
        {

            throw new NotImplementedException();
        }

        public override void DisConnect()
        {

            throw new NotImplementedException();
        }
        public override void Push()
        {

            throw new NotImplementedException();
        }
    }
}
