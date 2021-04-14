using InternetId.Common.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;

namespace InternetId.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(options => options
                    .UseStartup<Startup>()
                    .ConfigureAppConfiguration((webHostBuilderContext, configurationBuilder) =>
                    {
                        IFileInfo fileInfo = ConfigFileProvider.Singleton.GetFileInfo("/appsettings.json");
                        if (fileInfo.Exists)
                        {
                            Console.WriteLine($"Reading appsettings from: {fileInfo.PhysicalPath}");
                            configurationBuilder.AddJsonFile(
                                path: fileInfo.PhysicalPath,
                                optional: false,
                                reloadOnChange: true);
                        }
                    })
                );
    }
}
