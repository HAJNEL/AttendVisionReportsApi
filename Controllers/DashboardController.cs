using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis(
        [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetKpisAsync(dateFrom, dateTo, department, employee, User));

    [HttpGet("hourly-traffic")]
    public async Task<IActionResult> GetHourlyTraffic([FromQuery] string date, [FromQuery] string? department) =>
        Ok(await dashboardService.GetHourlyTrafficAsync(date, department, User));

    [HttpGet("monthly-attendance")]
    public async Task<IActionResult> GetMonthlyAttendance([FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetMonthlyAttendanceAsync(department, employee, User));

    [HttpGet("dept-breakdown")]
    public async Task<IActionResult> GetDeptBreakdown([FromQuery] string? department) =>
        Ok(await dashboardService.GetDeptBreakdownAsync(department, User));

    [HttpGet("monthly-traffic")]
    public async Task<IActionResult> GetMonthlyTraffic(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetMonthlyTrafficAsync(year, month, department, employee, User));

    [HttpGet("yearly-traffic")]
    public async Task<IActionResult> GetYearlyTraffic([FromQuery] int year, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetYearlyTrafficAsync(year, department, employee, User));

    [HttpGet("day-events")]
    public async Task<IActionResult> GetDayEvents([FromQuery] string date, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetDayEventsAsync(date, department, employee, User));

    [HttpGet("day-people")]
    public async Task<IActionResult> GetDayPeople([FromQuery] string date, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetDayPeopleAsync(date, department, employee, User));

}