using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Common
{
    public enum MessageType
    {
        FileChunk,
        ConsoleOutput,
        ProgressUpdate
    }

    public enum ResultMessageType
    {
        LoggedIn=1,
        LoginFailed=2,
        StreamError=3 
    }
    public class LoginInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginInfoHashed
    {
        public string  Username { get; set; }
        public byte[] PasswordHash { get; set; }
    }



    public  class NetworkMessage
    {
        public MessageType Mt { get; set; }
    }
}
