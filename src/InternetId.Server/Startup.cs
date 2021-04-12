using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InternetId.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
            => Configuration = configuration;

        public IConfiguration Configuration { get; }

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
            services.AddInternetIdSmtpEmailer(Configuration.GetSection("SmtpEmailer"));
            services.AddInternetIdCredentials(Configuration.GetSection("Credentials"), options => options.UseNpgsql(Configuration.GetConnectionString("Credentials")));
            services.AddInternetIdUsers(Configuration.GetSection("PwnedPasswordsClient"), options => options.UseNpgsql(Configuration.GetConnectionString("Users")));
            services.AddInternetIdServer(options => options.UseNpgsql(Configuration.GetConnectionString("OpenIddict")));
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
