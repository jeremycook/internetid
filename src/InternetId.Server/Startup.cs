using InternetId.Npgsql;
using InternetId.Server.Asp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using System.Net;

namespace InternetId.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            HostEnvironment = environment;
            ForwardedHeaders = Configuration.GetSection("ForwardedHeaders");
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostEnvironment { get; }
        public IConfigurationSection ForwardedHeaders { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            if (ForwardedHeaders.Exists())
            {
                services.Configure<ForwardedHeadersOptions>(ForwardedHeaders);
                services.Configure<ForwardedHeadersOptions>(binderOptions =>
                {
                    if (ForwardedHeaders.GetSection("KnownProxies").Get<string[]>() is string[] knownProxies)
                    {
                        foreach (var item in knownProxies)
                        {
                            if (IPAddress.TryParse(item, out var address))
                                binderOptions.KnownProxies!.Add(address!);
                        }
                    }
                });
            }

            services.AddControllersWithViews(options =>
            {
                options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
            });
            services.AddRazorPages(options =>
            {
                options.Conventions.Add(new PageRouteTransformerConvention(new SlugifyParameterTransformer()));
            });

            services.AddDbContext<AspDbContext>(options => options.UseSnakeCaseNamingConvention().UseNpgsql(NpgsqlConnectionBuilder.Build(Configuration, "Asp")));
            services.AddDataProtection().PersistKeysToDbContext<AspDbContext>();

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

            services.AddInternetIdCredentials(Configuration.GetSection("Credentials"), options => options.UseNpgsql(NpgsqlConnectionBuilder.Build(Configuration, "Credentials")));
            services.AddInternetIdUsers(Configuration.GetSection("PwnedPasswordsClient"), options => options.UseNpgsql(NpgsqlConnectionBuilder.Build(Configuration, "Users")));
            services.AddInternetIdServer(HostEnvironment, Configuration, options => options.UseNpgsql(NpgsqlConnectionBuilder.Build(Configuration, "OpenIddict")));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (ForwardedHeaders.Exists())
            {
                app.UseForwardedHeaders();
            }

            if (Configuration.GetSection("ForceHttps").Get<bool>() == true)
            {
                const string https = "https";
                app.Use((context, next) =>
                {
                    context.Request.Scheme = https;
                    return next(context);
                });
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("/error");
                //app.UseExceptionHandler("/error");
            }
            app.UseStaticFiles();

            app.UseSerilogRequestLogging(o => o.EnrichDiagnosticContext = Program.SerilogEnrichFromRequest);

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
