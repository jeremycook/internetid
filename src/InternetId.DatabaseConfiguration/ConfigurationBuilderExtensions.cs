using InternetId.DatabaseConfiguration;
using System.Data.Common;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddDatabaseConfigurationSource(this IConfigurationBuilder builder, DbConnection dbConnection, string selectSql)
        {
            return builder.Add(new DatabaseConfigurationSource(dbConnection, selectSql));
        }
    }
}
