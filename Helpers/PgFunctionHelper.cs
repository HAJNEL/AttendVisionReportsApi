using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Helpers
{
    public static class PgFunctionHelper
    {
        /// <summary>
        /// Dynamically calls a PostgreSQL set-returning function and maps the result to the specified type.
        /// </summary>
        /// <typeparam name="T">The return type (DTO or dynamic).</typeparam>
        /// <param name="conn">An open NpgsqlConnection.</param>
        /// <param name="functionName">The name of the function to call.</param>
        /// <param name="parameters">Dictionary of parameter names and values.</param>
        /// <returns>IEnumerable of T with the function results.</returns>
        public static async Task<IEnumerable<T>> CallFunctionAsync<T>(
            NpgsqlConnection conn,
            string functionName,
            IDictionary<string, object> parameters)
        {

            var paramNames = parameters.Keys.ToList();
            // Cast p_user_id as ::uuid, _date/p_date* as ::date, others as ::text
            var placeholders = string.Join(", ", paramNames.Select(p =>
                p == "p_user_id" ? $"@{p}::uuid"
                : (p.EndsWith("_date") || p.StartsWith("p_date")) ? $"@{p}::date"
                : $"@{p}::text"
            ));
            var sql = $"SELECT * FROM {functionName}({placeholders})";

            var dynamicParams = new DynamicParameters();
            foreach (var kvp in parameters)
                dynamicParams.Add(kvp.Key, kvp.Value);

            return await conn.QueryAsync<T>(sql, dynamicParams);
        }
    }
}
