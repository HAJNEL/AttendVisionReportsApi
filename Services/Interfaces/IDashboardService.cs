using AttendVisionReportsApi.DTOs;
using System.Security.Claims;

namespace AttendVisionReportsApi.Services
{
    public interface IDashboardService
    {
        Task<DashboardKpisResponse> GetKpisAsync(string dateFrom, string dateTo, string? department, string? employee, ClaimsPrincipal? user = null);
        Task<IEnumerable<dynamic>> GetHourlyTrafficAsync(string date, string? department, ClaimsPrincipal? user = null);
        Task<IEnumerable<dynamic>> GetMonthlyAttendanceAsync(string? department, string? employee, ClaimsPrincipal? user = null);
        Task<IEnumerable<dynamic>> GetDeptBreakdownAsync(string? department, ClaimsPrincipal? user = null);
        Task<IEnumerable<dynamic>> GetMonthlyTrafficAsync(int year, int month, string? department, string? employee, ClaimsPrincipal? user = null);
        Task<IEnumerable<dynamic>> GetYearlyTrafficAsync(int year, string? department, string? employee, ClaimsPrincipal? user = null);
        Task<IEnumerable<dynamic>> GetDayEventsAsync(string date, string? department, string? employee, ClaimsPrincipal? user = null);
        Task<IEnumerable<DayPersonRowResponse>> GetDayPeopleAsync(string date, string? department, string? employee, ClaimsPrincipal? user = null);
    }
}
