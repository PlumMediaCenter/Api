using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using PlumMediaCenter.Data;
using Dapper;
namespace PlumMediaCenter.Business
{
    public class BaseManager
    {
        public BaseManager(Manager manager = null)
        {
            this.Manager = manager;
        }

        public IDbConnection GetNewConnection()
        {
            return ConnectionManager.GetNewConnection();
        }
        public Manager Manager;

        public string BaseUrl
        {
            get
            {
                return this.Manager.BaseUrl;
            }
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
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var connection = GetNewConnection())
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
        public async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            using (var connection = GetNewConnection())
            {
                return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
            }
        }
    }
}