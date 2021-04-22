using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace InternetId.DatabaseConfiguration
{
    public class DatabaseConfigurationSource : IConfigurationSource
    {
        private readonly DbConnection dbConnection;
        private readonly string selectSql;

        public DatabaseConfigurationSource(DbConnection dbConnection, string selectSql)
        {
            this.dbConnection = dbConnection;
            this.selectSql = selectSql;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DatabaseConfigurationProvider(dbConnection, selectSql);
        }
    }
}
