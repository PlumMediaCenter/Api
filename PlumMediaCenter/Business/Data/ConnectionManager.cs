using System.Data;
using MySql.Data.MySqlClient;
namespace PlumMediaCenter.Data
{
    class ConnectionManager
    {
        private static string Username;
        private static string Password;

        public static void SetDbCredentials(string username, string password)
        {
            Username = username; 
            Password = password;
        }

        /// <summary>
        /// Get a new database connection
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static IDbConnection GetNewConnection(string username = null, string password = null, bool includeDatabase = true)
        {
            username = username == null ? Username : username;
            password = password == null ? Password : password;
            var dbString = includeDatabase ? "database=pmc;" : "";
            var connectionString = $"server=127.0.0.1;uid={username};pwd={password};{dbString}SslMode=None";
            var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            return connection;
        }
    }

}