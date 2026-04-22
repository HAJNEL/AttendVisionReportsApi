namespace AttendVisionReportsApi.Services
{
    public interface IReportsService
    {
        Task<IEnumerable<dynamic>> GetIssuesAsync(string dateFrom, string dateTo, string? department);
        Task<IEnumerable<dynamic>> GetClockingsAsync(string dateFrom, string dateTo, string? dept, string? user);
        Task<IEnumerable<string>> GetTimesheetUsersAsync(string dateFrom, string dateTo, string? dept);
        Task<IEnumerable<dynamic>> GetTimesheetAsync(string dateFrom, string dateTo, string? dept, string? user);
    }
}
