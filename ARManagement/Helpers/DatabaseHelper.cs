using Microsoft.Extensions.Options;
using Models;

namespace ARManagement.Helpers
{

    /// <summary>
    /// Database 介面
    /// </summary>
    public interface IDatabaseHelper
    {
        /// <summary>
        /// 取得連線
        /// </summary>
        /// <returns></returns>
        string GetPostgreSqlConnectionString();
    }

    public class DatabaseHelper : IDatabaseHelper
    {
        private PostgreSqlDBConfig _dbConfig;

        public DatabaseHelper(IOptions<PostgreSqlDBConfig> dbConfig)
        {
            _dbConfig = dbConfig.Value;
        }

        public string GetPostgreSqlConnectionString()
        {
            return _dbConfig.ConnectionString;
        }
    }
}
