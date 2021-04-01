using System.Threading.Tasks;

namespace InternetId.Common.Email
{
    public interface IEmailer
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage);
    }
}