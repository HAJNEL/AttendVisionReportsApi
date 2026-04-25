using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Helpers
{
    public static class PgProcedureHelper
    {
        /// <summary>
        /// Dynamically calls a PostgreSQL stored procedure (void or scalar) with the specified parameters.
        /// </summary>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="procedureName">The name of the procedure to call.</param>
        /// <param name="parameters">Dictionary of parameter names and values.</param>
        /// <returns>The number of rows affected (for void procedures), or the scalar result if any.</returns>
        public static async Task<int> CallProcedureAsync(
            NpgsqlConnection conn,
            string procedureName,
            IDictionary<string, object> parameters)
        {
            var paramNames = parameters.Keys;
            var placeholders = string.Join(", ", paramNames.Select(p => "@" + p));
            var sql = $"CALL {procedureName}({placeholders})";

            var dynamicParams = new DynamicParameters();
            foreach (var kvp in parameters)
                dynamicParams.Add(kvp.Key, kvp.Value);

            return await conn.ExecuteAsync(sql, dynamicParams);
        }
    }
}
