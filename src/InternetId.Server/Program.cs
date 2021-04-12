using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace InternetId.Server
{
    public static class Program
    {
        private const string containerAppsettingsPath = "/config/appsettings.json";

        public static void Main(string[] args) =>
            CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(options => options
                    .UseStartup<Startup>()
                    .ConfigureAppConfiguration((webHostBuilderContext, configurationBuilder) =>
                    {
                        if (webHostBuilderContext.HostingEnvironment.ContentRootPath.TrimEnd('/') == "/app" &&
                            File.Exists(containerAppsettingsPath))
                        {
                            Console.WriteLine($"Reading appsettings from: {containerAppsettingsPath}");
                            configurationBuilder.AddJsonFile(
                                path: containerAppsettingsPath,
                                optional: false,
                                reloadOnChange: true);
                        }

                        if (Environment.GetEnvironmentVariable("APPSETTINGS") is string appsettingsPath)
                        {
                            appsettingsPath = Path.GetFullPath(appsettingsPath);
                            Console.WriteLine($"Reading appsettings from: {appsettingsPath}");
                            configurationBuilder.AddJsonFile(
                                path: appsettingsPath,
                                optional: false,
                                reloadOnChange: true);
                        }
                    })
                );
    }
}
