using Microsoft.Extensions.Options;
using MimeKit;
using SmartBackup.Services;
using System;
using System.Collections.Generic;
using SmartBackup.Common;
using SmartBackup.Model;
using Serilog;

namespace SmartBackup
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfReader ec;
        readonly Serilog.ILogger log;

        public EmailSender(
            IConfReader emailConfig,
            Serilog.ILogger _log
            )
        {
            log = _log;
            this.ec = emailConfig;
        }

        public void SendEmail(string subject, string Message, string attachmentPath = null, Dictionary<string, string> AdditionalHeaders = null, string ReplyToEmail = null)

        {
            SMTPSettings smtpSettings = ec.Configuration?.SMTP;
            if (smtpSettings == null)
            {
                log.Warning("Email settings not available, email will not be sent");
                return;
            }

            if (string.IsNullOrWhiteSpace(smtpSettings.To))
            {
                throw new ArgumentException("No to address provided");
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException("No subject provided");
            }

            if (String.IsNullOrEmpty(Message))
            {
                throw new ArgumentException("no message provided");
            }

            var m = new MimeMessage();

            m.From.Add(new MailboxAddress(smtpSettings.FromName ?? smtpSettings.FromName ?? "", smtpSettings.From));
            m.To.Add(new MailboxAddress("", smtpSettings.To));
            m.Subject = subject;

            if (AdditionalHeaders != null)
                foreach (KeyValuePair<string, string> hdr in AdditionalHeaders)
                {
                    string key = hdr.Key;
                    if (key.Equals("From")) continue;
                    if (key.Equals("To")) continue;
                    m.Headers.Add(key, hdr.Value);
                }

            BodyBuilder bodyBuilder = new BodyBuilder();
            string processedBody = null;

            processedBody = Message;
            if (!String.IsNullOrEmpty(attachmentPath))
                bodyBuilder.Attachments.Add(attachmentPath);

            bodyBuilder.HtmlBody = processedBody;

            m.Body = bodyBuilder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                // client.Connect(ec.Host, ec.Port, ec.UseSSl);

                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect(smtpSettings.Server, smtpSettings.Port, smtpSettings.UseSSL);

                // Note: since we don't have an OAuth2 token, disable
                // the XOAUTH2 authentication mechanism.
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                // Note: only needed if the SMTP server requires authentication
                if (!String.IsNullOrEmpty(smtpSettings.Username))
                {
                    client.Authenticate(smtpSettings.Username, smtpSettings.Password);
                }
                client.Send(m);
            }
        }
    }
}
