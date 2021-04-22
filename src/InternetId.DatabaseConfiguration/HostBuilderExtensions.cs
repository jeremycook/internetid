using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Configuration
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureNpgsqlConfigurationProvider(this IHostBuilder hostBuilder)
        {

            return hostBuilder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddNpgsqlConfigurationSource();
            });
        }
    }
}
