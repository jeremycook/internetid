using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternetId.Server.Areas.Identity
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IOptions<SmtpEmailSenderOptions> smtpEmailSenderOptions;
        private readonly IOptions<InternetIdServerOptions> internetIdServerOptions;

        public SmtpEmailSender(IOptions<SmtpEmailSenderOptions> smtpEmailSenderOptions, IOptions<InternetIdServerOptions> internetIdServerOptions)
        {
            this.smtpEmailSenderOptions = smtpEmailSenderOptions;
            this.internetIdServerOptions = internetIdServerOptions;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using var message = new MailMessage(internetIdServerOptions.Value.FromEmailAddress, email)
            {
                Subject = string.Format("{0}: {1}", internetIdServerOptions.Value.Title, subject),
                Body = string.Format(internetIdServerOptions.Value.EmailFormat, HtmlEncoder.Default.Encode(subject), htmlMessage),
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
