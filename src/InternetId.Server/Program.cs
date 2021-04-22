using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace InternetId.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = CreateLogger(configuration);

            try
            {
                Log.Information("Starting up");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureNpgsqlConfigurationProvider()
                .ConfigureWebHostDefaults(options => options.UseStartup<Startup>())
                .UseSerilog();

        private static ILogger CreateLogger(IConfiguration configuration)
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console();

            if (configuration.GetValue<string>("Seq:ServerUrl") is string seqServerUrl)
            {
                loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: configuration.GetValue<string?>("Seq:ApiKey"));
            }

            return loggerConfiguration.CreateLogger();
        }
    }
}
