using System.Globalization;
using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AttendVisionReportsApi.Services
{
    public class ReportsService(NpgsqlDataSource dataSource, AppDbContext context) : IReportsService
    {

    public async Task<IEnumerable<dynamic>> GetIssuesAsync(string dateFrom, string dateTo, string? department, string? employeeId, string? employeeType, Guid userId)
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
        { "p_employee_type", string.IsNullOrWhiteSpace(employeeType) ? null : employeeType },
        { "p_user_id", userId }
      };
      return await Helpers.PgFunctionHelper.CallFunctionAsync<dynamic>(conn, "get_attendance_issues", parameters);
    }

        public async Task<IEnumerable<dynamic>> GetClockingsAsync(string dateFrom, string dateTo, string? dept, string? employeeId, string? employeeType, Guid userId)
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
              { "p_employee_type", string.IsNullOrWhiteSpace(employeeType) ? null : employeeType },
              { "p_user_id", userId }
            };
            return await Helpers.PgFunctionHelper.CallFunctionAsync<dynamic>(conn, "get_clockings", parameters);
        }

        public async Task<IEnumerable<dynamic>> GetTimesheetAsync(string dateFrom, string dateTo, string? dept, string? employeeId, string? employeeType, Guid userId)
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
            { "p_employee_type", string.IsNullOrWhiteSpace(employeeType) ? null : employeeType },
            { "p_user_id", userId }
          };
          return await Helpers.PgFunctionHelper.CallFunctionAsync<dynamic>(conn, "get_timesheet", parameters);
        }

        public async Task<IEnumerable<dynamic>> GetSageTimesheetAsync(string dateFrom, string dateTo, string? dept, string? employeeId, string? employeeType, Guid userId)
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
            { "p_employee_type", string.IsNullOrWhiteSpace(employeeType) ? null : employeeType },
            { "p_user_id", userId }
          };
          return await Helpers.PgFunctionHelper.CallFunctionAsync<dynamic>(conn, "get_sage_timesheet", parameters);
        }

        // ── Report period config ──────────────────────────────────────────────

        public async Task<ReportConfigDto> GetReportConfigAsync(Guid userId)
        {
            var entity = await GetConfigEntityAsync(userId);
            return MapConfigToDto(entity);
        }

        public async Task<ReportConfigDto> SaveReportConfigAsync(ReportConfigDto dto, Guid userId)
        {
            var companyId = await ResolveCompanyIdAsync(userId)
                ?? throw new InvalidOperationException("No company is linked to the current user.");

            var entity = await context.ReportConfigs
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (entity == null)
            {
                entity = new ReportConfig { Id = Guid.NewGuid(), CompanyId = companyId };
                context.ReportConfigs.Add(entity);
            }

            entity.MonthStartDay = dto.MonthStartDay;
            entity.MonthEndDay = dto.MonthEndDay;
            entity.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return MapConfigToDto(entity);
        }

        private async Task<ReportConfig> GetConfigEntityAsync(Guid userId)
        {
            var companyId = await ResolveCompanyIdAsync(userId);
            if (companyId == null) return new ReportConfig { CompanyId = Guid.Empty };

            var entity = await context.ReportConfigs
                .FirstOrDefaultAsync(c => c.CompanyId == companyId.Value);
            return entity ?? new ReportConfig { CompanyId = companyId.Value };
        }

        private async Task<Guid?> ResolveCompanyIdAsync(Guid userId)
        {
            var deptIds = await context.DepartmentUsers
                .Where(du => du.UserId == userId)
                .Select(du => du.DepartmentId)
                .ToListAsync();

            if (deptIds.Count == 0) return null;

            return await context.Departments
                .Where(d => deptIds.Contains(d.Id) && d.CompanyId != null)
                .Select(d => d.CompanyId)
                .FirstOrDefaultAsync();
        }

        private static ReportConfigDto MapConfigToDto(ReportConfig c) => new(
            c.CompanyId == Guid.Empty ? null : c.CompanyId,
            c.MonthStartDay,
            c.MonthEndDay
        );
    }
}
