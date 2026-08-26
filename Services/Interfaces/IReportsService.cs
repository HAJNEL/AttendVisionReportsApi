using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IReportsService
    {
        Task<IEnumerable<dynamic>> GetIssuesAsync(string dateFrom, string dateTo, string? department, string? employeeId, Guid? attendanceGroupId, Guid userId);
        Task<IEnumerable<dynamic>> GetClockingsAsync(string dateFrom, string dateTo, string? dept, string? employeeId, Guid? attendanceGroupId, Guid userId);
        Task<IEnumerable<dynamic>> GetTimesheetAsync(string dateFrom, string dateTo, string? dept, string? employeeId, Guid? attendanceGroupId, Guid userId);
        Task<IEnumerable<dynamic>> GetSageTimesheetAsync(string dateFrom, string dateTo, string? dept, string? employeeId, Guid? attendanceGroupId, Guid userId);
        Task<ReportConfigDto> GetReportConfigAsync(Guid userId);
        Task<ReportConfigDto> SaveReportConfigAsync(ReportConfigDto dto, Guid userId);
    }
}
