using InternetId.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StartupExtensions
    {
        public static void AddInternetIdCredentials(this IServiceCollection services, IConfigurationSection credentialsOptions, Action<DbContextOptionsBuilder> credentialsDbContextOptionsBuilder)
        {
            services.Configure<CredentialsOptions>(credentialsOptions);
            services.AddDbContext<CredentialsDbContext>(credentialsDbContextOptionsBuilder);
            services.AddScoped<ICredentialsDbContext, CredentialsDbContext>(o => o.GetRequiredService<CredentialsDbContext>());
            services.AddScoped<CredentialManager>();
        }
    }
}
