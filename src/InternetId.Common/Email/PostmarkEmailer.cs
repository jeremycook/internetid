using InternetId.Common.Text;
using Microsoft.Extensions.Options;
using PostmarkDotNet;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternetId.Common.Email
{
    public class PostmarkEmailer : IEmailer
    {
        private readonly IOptions<PostmarkEmailerOptions> postmarkEmailerOptions;
        private readonly IOptions<InternetIdOptions> internetIdOptions;

        public PostmarkEmailer(IOptions<PostmarkEmailerOptions> postmarkEmailerOptions, IOptions<InternetIdOptions> internetIdOptions)
        {
            this.postmarkEmailerOptions = postmarkEmailerOptions;
            this.internetIdOptions = internetIdOptions;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For more options, see https://account.postmarkapp.com/servers/6427145/streams/outbound/get-started

            string htmlBody = string.Format(internetIdOptions.Value.EmailBodyFormat, HtmlEncoder.Default.Encode(subject), htmlMessage);
            var message = new PostmarkMessage()
            {
                To = email,
                From = internetIdOptions.Value.EmailFromAddress,
                TrackOpens = true,
                Subject = subject,
                TextBody = Html.Strip(htmlBody),
                HtmlBody = htmlBody,
                MessageStream = "outbound",
            };

            var client = new PostmarkClient(postmarkEmailerOptions.Value.ClientId);
            var sendResult = await client.SendMessageAsync(message);

            for (int i = 0; i < 5; i++)
            {
                if (sendResult.Status == PostmarkStatus.Success)
                {
                    return;
                }

                // Retry with exponential back off
                // 100, 300, 900, 2700, 8100
                const int initialWait = 100, @base = 3;
                await Task.Delay((int)(initialWait * Math.Pow(@base, i)));

                sendResult = await client.SendMessageAsync(message);
            }

            throw new InvalidOperationException("Failed to send email.");
        }
    }
}
