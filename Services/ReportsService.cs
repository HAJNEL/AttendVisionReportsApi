using System.Globalization;
using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Services
{
    public class ReportsService(NpgsqlDataSource dataSource) : IReportsService
    {

    public async Task<IEnumerable<dynamic>> GetIssuesAsync(string dateFrom, string dateTo, string? department, string? employeeId, Guid userId)
    {
      var df = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
      var dt = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
      using var conn = dataSource.CreateConnection();
      var parameters = new Dictionary<string, object>
      {
        { "p_date_from", df.ToString("yyyy-MM-dd") },
        { "p_date_to", dt.ToString("yyyy-MM-dd") },
        { "p_dept", department == "all" ? null : department },
        { "p_employee_id", string.IsNullOrWhiteSpace(employeeId) ? null : employeeId },
        { "p_user_id", userId }
      };
      return await Helpers.PgFunctionHelper.CallFunctionAsync<dynamic>(conn, "get_attendance_issues", parameters);
    }

        public async Task<IEnumerable<dynamic>> GetClockingsAsync(string dateFrom, string dateTo, string? dept, string? employeeId, Guid userId)
        {
            var df = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dt = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var conn = dataSource.CreateConnection();
            var parameters = new Dictionary<string, object>
            {
              { "p_date_from", df.ToString("yyyy-MM-dd") },
              { "p_date_to", dt.ToString("yyyy-MM-dd") },
              { "p_dept", dept == "all" ? null : dept },
              { "p_employee_id", string.IsNullOrWhiteSpace(employeeId) ? null : employeeId },
              { "p_user_id", userId }
            };
            return await Helpers.PgFunctionHelper.CallFunctionAsync<dynamic>(conn, "get_clockings", parameters);
        }

        public async Task<IEnumerable<dynamic>> GetTimesheetAsync(string dateFrom, string dateTo, string? dept, string? employeeId, Guid userId)
        {
          var df = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
          var dt = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
          using var conn = dataSource.CreateConnection();
          var parameters = new Dictionary<string, object>
          {
            { "p_date_from", df.ToString("yyyy-MM-dd") },
            { "p_date_to", dt.ToString("yyyy-MM-dd") },
            { "p_dept", dept == "all" ? null : dept },
            { "p_employee_id", string.IsNullOrWhiteSpace(employeeId) ? null : employeeId },
            { "p_user_id", userId }
          };
          return await Helpers.PgFunctionHelper.CallFunctionAsync<dynamic>(conn, "get_timesheet", parameters);
        }
    }
}
