using InternetId.Common.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace InternetId.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            HostEnvironment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            if (Configuration.GetSection("Seq") is IConfigurationSection seq &&
                seq.Exists())
            {
                services.AddLogging(loggingBuilder => loggingBuilder
                    .AddSeq(
                        serverUrl: seq.GetValue<string>("ServerUrl"),
                        apiKey: seq.GetValue<string>("ApiKey")
                    )
                );
            }

            services.AddControllersWithViews(options =>
            {
                options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
            });
            services.AddRazorPages(options =>
            {
                options.Conventions.Add(new PageRouteTransformerConvention(new SlugifyParameterTransformer()));
            });

            services.AddInternetId(Configuration.GetSection("InternetId"));
            services.AddInternetIdHasher();

            IConfigurationSection postmarkEmailerOptions = Configuration.GetSection("PostmarkEmailer");
            if (postmarkEmailerOptions.Exists())
            {
                services.AddInternetIdPostmarkEmailer(postmarkEmailerOptions);
            }
            else
            {
                services.AddInternetIdSmtpEmailer(Configuration.GetSection("SmtpEmailer"));
            }

            services.AddInternetIdCredentials(Configuration.GetSection("Credentials"), options => options.UseNpgsql(CreateConnection("Credentials")));
            services.AddInternetIdUsers(Configuration.GetSection("PwnedPasswordsClient"), options => options.UseNpgsql(CreateConnection("Users")));
            services.AddInternetIdServer(options => options.UseNpgsql(CreateConnection("OpenIddict")));
        }

        private Npgsql.NpgsqlConnection CreateConnection(string connectionStringName)
        {
            var credentialsConnection = new Npgsql.NpgsqlConnection(Configuration.GetConnectionString(connectionStringName));

            if (ConfigFileProvider.Singleton.ReadAllBytes($"{connectionStringName.ToLower()}-ca.crt") is byte[] rawData)
            {
                var caCert = new X509Certificate2(rawData);
                credentialsConnection.UserCertificateValidationCallback = CreateUserCertificateValidationCallback(caCert);
            }

            return credentialsConnection;
        }

        private static RemoteCertificateValidationCallback CreateUserCertificateValidationCallback(X509Certificate2 caCert)
        {
            return (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
            {
                if (certificate is null)
                {
                    throw new ArgumentNullException(nameof(certificate));
                }

                X509Chain caCertChain = new X509Chain();
                caCertChain.ChainPolicy = new X509ChainPolicy()
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    RevocationFlag = X509RevocationFlag.EntireChain
                };
                caCertChain.ChainPolicy.ExtraStore.Add(caCert);

                X509Certificate2 serverCert = new X509Certificate2(certificate);

                caCertChain.Build(serverCert);
                if (caCertChain.ChainStatus.Length == 0)
                {
                    // No errors
                    return true;
                }

                foreach (X509ChainStatus status in caCertChain.ChainStatus)
                {
                    // Check if we got any errors other than UntrustedRoot (which we will always get if we don't install the CA cert to the system store)
                    if (status.Status != X509ChainStatusFlags.UntrustedRoot)
                    {
                        return false;
                    }
                }

                return true;
            };
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("/error");
                //app.UseExceptionHandler("/error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
            });
        }
    }
}
