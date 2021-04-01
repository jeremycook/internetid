using InternetId.Credentials;
using InternetId.OpenIddict.Data;
using InternetId.Users.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace InternetId.Server
{
    public class StartupWorker : IHostedService
    {
        private readonly IWebHostEnvironment env;
        private readonly IServiceProvider serviceProvider;

        public StartupWorker(IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            this.env = env;
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!env.IsDevelopment())
            {
                return;
            }

            using var scope = serviceProvider.CreateScope();

            await scope.ServiceProvider.GetRequiredService<CredentialsDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<OpenIddictDbContext>().Database.EnsureCreatedAsync();

            var context = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            await context.Database.EnsureCreatedAsync();

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            if (await manager.FindByClientIdAsync("mvc") == null)
            {
                await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "mvc",
                    ClientSecret = "901564A5-E7FE-42CB-B10D-61EF6A8F3654",
                    ConsentType = ConsentTypes.Explicit,
                    DisplayName = "MVC client application",
                    PostLogoutRedirectUris =
                    {
                        new Uri("https://localhost:44338/signout-callback-oidc")
                    },
                    RedirectUris =
                    {
                        new Uri("https://localhost:44338/signin-oidc")
                    },
                    Permissions =
                    {
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.Logout,
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.ResponseTypes.Code,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Roles,
                        Permissions.Prefixes.Scope + "demo_api"
                    },
                    Requirements =
                    {
                        Requirements.Features.ProofKeyForCodeExchange
                    }
                });
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
