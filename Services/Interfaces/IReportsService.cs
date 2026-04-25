namespace AttendVisionReportsApi.Services
{
    public interface IReportsService
    {
        Task<IEnumerable<dynamic>> GetIssuesAsync(string dateFrom, string dateTo, string? department, string? employeeId, Guid userId);
        Task<IEnumerable<dynamic>> GetClockingsAsync(string dateFrom, string dateTo, string? dept, string? employeeId, Guid userId);
        Task<IEnumerable<dynamic>> GetTimesheetAsync(string dateFrom, string dateTo, string? dept, string? employeeId, Guid userId);
    }
}
