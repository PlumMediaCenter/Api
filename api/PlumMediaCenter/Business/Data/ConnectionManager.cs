using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;

namespace PlumMediaCenter.Business.Data
{
    public class ConnectionManager
    {
        public static string Username;
        public static string Password;
        public static string Host;
        public static string DbName;

        public static void SetDbConnectionInfo(string username, string password, string host, string dbName)
        {
            Username = username;
            Password = password;
            Host = host;
            DbName = dbName;
        }

        // public static IDbConnection CreateConnection(string username = null, string password = null, bool includeDatabase = true)
        // {
        //     return null;
        // }

        /// <summary>
        /// Get a new database connection. This also supports logging in as root by passing includeInDatabase = false
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static IDbConnection CreateConnection(string username = null, string password = null, bool includeDatabase = true)
        {
            username = username == null ? Username : username;
            password = password == null ? Password : password;
            var dbString = includeDatabase ? $"database={DbName};" : "";
            var connectionString = $"server={Host};uid={username};pwd={password};{dbString}SslMode=None";
            var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            return connection;
        }


        /// <summary>
        /// Wrapper around dapper QueryAsync that creates and closes a connection automatically
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
            }
        }

        /// <summary>
        /// Wrapper around dapper ExecuteAsync that creates and closes a connection automatically
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var connection = CreateConnection())
            {
                return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
            }
        }
    }
}