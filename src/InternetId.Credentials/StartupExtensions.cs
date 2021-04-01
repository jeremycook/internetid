using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InternetId.Credentials
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
