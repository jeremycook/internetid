using InternetId.OpenIddict.Data;
using InternetId.Server;
using InternetId.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Security.Cryptography.X509Certificates;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StartupExtensions
    {
        public static void AddInternetIdServer(
            this IServiceCollection services,
            IHostEnvironment environment,
            IConfiguration configuration,
            Action<DbContextOptionsBuilder> openIddictDbContextOptionsBuilder)
        {
            services.AddCookiePolicy(options => options.MinimumSameSitePolicy = AspNetCore.Http.SameSiteMode.Lax);

            services.AddHttpContextAccessor();

            services.AddScoped<SignInManager>();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.SameSite = AspNetCore.Http.SameSiteMode.Strict;

                    options.AccessDeniedPath = "/access-denied";
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";

                    //// Controls the lifetime of the authentication session and cookie
                    //// when AuthenticationProperties.IsPersistent is set to true.
                    //options.ExpireTimeSpan = TimeSpan.FromDays(2);
                    //options.SlidingExpiration = true;
                });

            services.AddDbContext<OpenIddictDbContext>(openIddictDbContextOptionsBuilder);

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddOpenIddict()

                // Register the OpenIddict core components.
                .AddCore(options =>
                {
                    // Configure OpenIddict to use the Entity Framework Core stores and models.
                    // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                    options.UseEntityFrameworkCore()
                           .UseDbContext<OpenIddictDbContext>();
                })

                // Register the OpenIddict server components.
                .AddServer(options =>
                {
                    // Enable the authorization, logout, token and userinfo endpoints.
                    options.SetAuthorizationEndpointUris("/connect/authorize")
                           .SetLogoutEndpointUris("/connect/logout")
                           .SetTokenEndpointUris("/connect/token")
                           .SetUserinfoEndpointUris("/connect/userinfo");

                    // Mark the "email", "profile" and "roles" scopes as supported scopes.
                    options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

                    // Note: this sample only uses the authorization code flow but you can enable
                    // the other flows if you need to support implicit, password or client credentials.
                    options.AllowAuthorizationCodeFlow();

                    // Register the signing and encryption credentials.
                    if (environment.IsDevelopment())
                    {
                        options.AddDevelopmentEncryptionCertificate()
                               .AddDevelopmentSigningCertificate();
                    }
                    else
                    {
                        var encryptionCertificate = X509Certificate2.CreateFromPem(
                            certPem: configuration.GetValue<string?>("ENCRYPTION_CERTIFICATE") ?? throw new Exception("Missing ENCRYPTION_CERTIFICATE configuration in PEM format. See Readme.md for more info."),
                            keyPem: configuration.GetValue<string>("ENCRYPTION_KEY") ?? throw new Exception("Missing ENCRYPTION_KEY configuration in PEM format. See Readme.md for more info.")
                        );

                        var signingCertificate = X509Certificate2.CreateFromPem(
                            certPem: configuration.GetValue<string?>("SIGNING_CERTIFICATE") ?? throw new Exception("Missing SIGNING_CERTIFICATE configuration in PEM format. See Readme.md for more info."),
                            keyPem: configuration.GetValue<string>("SIGNING_KEY") ?? throw new Exception("Missing SIGNING_KEY configuration in PEM format. See Readme.md for more info.")
                        );

                        options.AddEncryptionCertificate(encryptionCertificate)
                               .AddSigningCertificate(signingCertificate);
                    }

                    // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                    options.UseAspNetCore()
                           .EnableAuthorizationEndpointPassthrough()
                           .EnableLogoutEndpointPassthrough()
                           .EnableTokenEndpointPassthrough()
                           .EnableUserinfoEndpointPassthrough()
                           .EnableStatusCodePagesIntegration();
                })

                // Register the OpenIddict validation components.
                .AddValidation(options =>
                {
                    // Import the configuration from the local OpenIddict server instance.
                    options.UseLocalServer();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

            // Register the worker responsible of seeding the database with the sample clients.
            // Note: in a real world application, this step should be part of a setup script.
            services.AddHostedService<StartupWorker>();
        }
    }
}
