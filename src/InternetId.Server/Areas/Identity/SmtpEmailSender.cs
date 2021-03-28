using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace InternetId.Server.Areas.Identity
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IOptions<SmtpEmailSenderOptions> smtpEmailSenderOptions;

        public SmtpEmailSender(IOptions<SmtpEmailSenderOptions> smtpEmailSenderOptions)
        {
            this.smtpEmailSenderOptions = smtpEmailSenderOptions;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            MailAddress from = new MailAddress(smtpEmailSenderOptions.Value.From);
            MailAddress to = new MailAddress(email);
            using var message = new MailMessage(from, to)
            {
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
            };

            using var smtpClient = new SmtpClient();
            Configure(smtpClient);
            await smtpClient.SendMailAsync(message);
        }

        private void Configure(SmtpClient smtpClient)
        {
            var options = smtpEmailSenderOptions.Value;

            smtpClient.Host = options.Host;
            if (options.Port != null)
            {
                smtpClient.Port = options.Port.Value;
            }
            smtpClient.EnableSsl = options.EnableSsl;
            if (options.Username != null)
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(options.Username, options.Password);
            }
            else
            {
                smtpClient.UseDefaultCredentials = true;
            }
        }
    }
}
