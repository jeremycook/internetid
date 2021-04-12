using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace InternetId.Server
{
    public static class Program
    {
        public static void Main(string[] args) =>
            CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(options => options
                    .UseStartup<Startup>()
                    .ConfigureAppConfiguration((webHostBuilderContext, configurationBuilder) =>
                    {
                        if (Environment.GetEnvironmentVariable("APPSETTINGS") is string configPath)
                        {
                            configPath = Path.GetFullPath(configPath);
                            configurationBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
                        }
                    })
                );
    }
}
