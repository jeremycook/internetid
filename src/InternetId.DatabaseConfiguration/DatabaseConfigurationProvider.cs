using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using System.Linq;

namespace InternetId.DatabaseConfiguration
{
    public class DatabaseConfigurationProvider : ConfigurationProvider
    {
        private readonly DbConnection dbConnection;
        private readonly string selectSql;

        public DatabaseConfigurationProvider(DbConnection dbConnection, string selectSql)
        {
            this.dbConnection = dbConnection;
            this.selectSql = selectSql;
        }

        public override void Load()
        {
            var records = dbConnection.Query<Appsettings>(selectSql);
            Data = records.ToDictionary(c => c.Id, c => c.Value);
        }
    }
}
