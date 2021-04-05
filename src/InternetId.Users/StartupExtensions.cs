using InternetId.Users.Data;
using InternetId.Users.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PwnedPasswords.Client;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StartupExtensions
    {
        public static void AddInternetIdUsers(this IServiceCollection services, IConfigurationSection pwnedPasswordsClientConfiguration, Action<DbContextOptionsBuilder> usersDbContextOptionsBuilder)
        {
            services.Configure<PwnedPasswordsClientOptions>(pwnedPasswordsClientConfiguration);
            services.AddPwnedPasswordHttpClient();

            services.AddDbContext<UsersDbContext>(usersDbContextOptionsBuilder);

            services.AddScoped<EmailService>();
            services.AddScoped<PasswordResetService>();
            services.AddScoped<PasswordService>();
            services.AddScoped<UserClientManager>();
            services.AddScoped<UserFinder>();
        }
    }
}
