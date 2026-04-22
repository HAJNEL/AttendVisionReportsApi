using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IDashboardService
    {
        Task<DashboardKpisResponse> GetKpisAsync(string dateFrom, string dateTo, string? department, string? employee);
        Task<IEnumerable<dynamic>> GetHourlyTrafficAsync(string date, string? department);
        Task<IEnumerable<dynamic>> GetMonthlyAttendanceAsync(string? department, string? employee);
        Task<IEnumerable<dynamic>> GetDeptBreakdownAsync(string? department);
        Task<IEnumerable<dynamic>> GetMonthlyTrafficAsync(int year, int month, string? department, string? employee);
        Task<IEnumerable<dynamic>> GetYearlyTrafficAsync(int year, string? department, string? employee);
        Task<IEnumerable<dynamic>> GetDayEventsAsync(string date, string? department, string? employee);
        Task<IEnumerable<DayPersonRowResponse>> GetDayPeopleAsync(string date, string? department, string? employee);
        Task<int> GetOnBreakNowCountAsync(string date, string? department, string? employee);
    }
}
