using System.Data;
using Dapper;
using System.Linq;
using System.Threading.Tasks;

namespace PlumMediaCenter.Data
{
    public static class IdbConnectionExtensions
    {

        /// <summary>
        /// Get the id of the last inserted item
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static async Task<ulong?> GetLastInsertIdAsync(this IDbConnection connection)
        {
            var id = await connection.QueryAsync<ulong?>("select last_insert_id();");
            return id.FirstOrDefault();
        }
    }
}