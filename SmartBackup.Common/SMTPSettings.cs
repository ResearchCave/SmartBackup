using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBackup.Model
{
    public class SMTPSettings  
    {
        [NotNull]
        [DisplayName("Mail Server")]
        public string Server { get; set; }

        [DefaultValue(25)]
        [DisplayName("Mail Server Port")]
        public ushort Port { get; set; } = 25;

        [EmailAddress(ErrorMessage ="Value should be a valid email address")]
        [Description("Mail address which report will be sent from")]
        [DisplayName("From E-Mail")]
        public string From { get; set; }
        [EmailAddress(ErrorMessage = "Value should be a valid email address")]
        [Description("Mail address to send backup report")]
        [DisplayName("To E-Mail")]
        public string To { get; set; }
        [Description("Name to be shown on From E-mail")]
        [DisplayName("From E-Mail Name")]
        public string FromName { get; set; }
        [DefaultValue(true )]
        public bool UseSSL { get; set; }=false;
        public string Username { get; set; }
        public string Password { get; set; }

    }
}
