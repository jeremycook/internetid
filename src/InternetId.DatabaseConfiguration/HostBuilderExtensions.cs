using InternetId.Npgsql;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Microsoft.Extensions.Configuration
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureNpgsqlConfigurationProvider(this IHostBuilder hostBuilder)
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            return hostBuilder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                // Pull configuration from environment variables or
                // other sources that have already been configured.
                var tempConfiguration = configurationBuilder.Build();

                NpgsqlConnection dbConnection = NpgsqlConnectionBuilder.Build(tempConfiguration, "Configuration");

                string selectSql =
                    tempConfiguration.GetValue<string?>("ConnectionStrings:ConfigurationSelectSql") ??
                    tempConfiguration.GetValue<string?>("CONFIGURATION_SELECT_SQL") ??
                    "SELECT id, value FROM public.appsettings";

                configurationBuilder.AddDatabaseConfigurationSource(dbConnection, selectSql: selectSql);
            });
        }
    }
}
