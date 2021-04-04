using InternetId.Common;
using InternetId.Common.Crypto;
using InternetId.Common.Email;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StartupExtensions
    {
        public static void AddInternetId(this IServiceCollection services, IConfigurationSection internetIdOptions)
        {
            services.Configure<InternetIdOptions>(internetIdOptions);
        }

        public static void AddInternetIdHasher(this IServiceCollection services)
        {
            services.AddScoped<Hasher>();
        }

        public static void AddInternetIdSmtpEmailer(this IServiceCollection services, IConfigurationSection smtpEmailerOptions)
        {
            services.Configure<SmtpEmailerOptions>(smtpEmailerOptions);
            services.AddScoped<IEmailer, SmtpEmailer>();
        }
    }
}
