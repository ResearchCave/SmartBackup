using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartBackup.Common
{
    public interface IEmailSender
    {
        public void SendEmail(string subject, string Message, string attachmentPath = null, Dictionary<string, string> AdditionalHeaders = null, string ReplyToEmail = null);
    }
}
