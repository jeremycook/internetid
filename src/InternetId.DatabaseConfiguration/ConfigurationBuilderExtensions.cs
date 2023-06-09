﻿using InternetId.DatabaseConfiguration;
using InternetId.Npgsql;
using Npgsql;
using System.Data.Common;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddDatabaseConfigurationSource(this IConfigurationBuilder builder, DbConnection dbConnection, string selectSql)
        {
            return builder.Add(new DatabaseConfigurationSource(dbConnection, selectSql));
        }

        /// <summary>
        /// Adds an NPGSQL configuration source if a configuration database connection string exists.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="fallbackSelectSql"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddNpgsqlConfigurationSource(this IConfigurationBuilder builder, string fallbackSelectSql = "SELECT id, value FROM public.appsettings")
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            var tempConfiguration = builder.Build();

            if (!NpgsqlConnectionBuilder.ConnectionStringExists(tempConfiguration, "Configuration"))
            {
                return builder;
            }

            NpgsqlConnection dbConnection = NpgsqlConnectionBuilder.Build(tempConfiguration, "Configuration");

            string selectSql =
                tempConfiguration.GetValue<string?>("ConnectionStrings:ConfigurationSelectSql") ??
                tempConfiguration.GetValue<string?>("CONFIGURATION_SELECT_SQL") ??
                fallbackSelectSql;

            return builder.AddDatabaseConfigurationSource(dbConnection, selectSql);
        }
    }
}
