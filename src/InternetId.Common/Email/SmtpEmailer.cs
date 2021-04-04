using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternetId.Common.Email
{
    public class SmtpEmailer : IEmailer
    {
        private readonly IOptions<SmtpEmailerOptions> smtpEmailerOptions;
        private readonly IOptions<InternetIdOptions> internetIdOptions;

        public SmtpEmailer(IOptions<SmtpEmailerOptions> smtpEmailerOptions, IOptions<InternetIdOptions> internetIdOptions)
        {
            this.smtpEmailerOptions = smtpEmailerOptions;
            this.internetIdOptions = internetIdOptions;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using var message = new MailMessage(internetIdOptions.Value.EmailFromAddress, email)
            {
                Subject = string.Format(internetIdOptions.Value.EmailSubjectFormat, subject),
                Body = string.Format(internetIdOptions.Value.EmailBodyFormat, HtmlEncoder.Default.Encode(subject), htmlMessage),
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
            var options = smtpEmailerOptions.Value;

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
