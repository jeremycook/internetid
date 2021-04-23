using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
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
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext()
                .WriteTo.Console();

            if (configuration.GetValue<string>("Seq:ServerUrl") is string seqServerUrl)
            {
                loggerConfiguration.WriteTo.Seq(seqServerUrl,
                    apiKey: configuration.GetValue<string?>("Seq:ApiKey"),
                    controlLevelSwitch: levelSwitch);
            }

            return loggerConfiguration.CreateLogger();
        }

        public static void SerilogEnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
        {
            var request = httpContext.Request;

            // Set all the common properties available for every request
            diagnosticContext.Set("Host", request.Host);
            diagnosticContext.Set("Protocol", request.Protocol);
            diagnosticContext.Set("Scheme", request.Scheme);
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);

            // Set the content-type of the Response at this point
            diagnosticContext.Set("ContentType", httpContext.Response.ContentType);

            if (httpContext.User.FindFirst("sub")?.Value is string sub)
            {
                diagnosticContext.Set("Sub", sub);
            }

            // Retrieve the IEndpointFeature selected for the request
            var endpoint = httpContext.GetEndpoint();
            if (endpoint is not null)
            {
                diagnosticContext.Set("EndpointName", endpoint.DisplayName);
            }
        }
    }
}
