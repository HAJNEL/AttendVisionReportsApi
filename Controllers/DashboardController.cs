using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{

    [HttpGet("on-break-now")]
    public async Task<IActionResult> GetOnBreakNow(
        [FromQuery] string date,
        [FromQuery] string? department,
        [FromQuery] string? employee) =>
        Ok(await dashboardService.GetOnBreakNowCountAsync(date, department, employee));

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis(
        [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetKpisAsync(dateFrom, dateTo, department, employee));

    [HttpGet("hourly-traffic")]
    public async Task<IActionResult> GetHourlyTraffic([FromQuery] string date, [FromQuery] string? department) =>
        Ok(await dashboardService.GetHourlyTrafficAsync(date, department));

    [HttpGet("monthly-attendance")]
    public async Task<IActionResult> GetMonthlyAttendance([FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetMonthlyAttendanceAsync(department, employee));

    [HttpGet("dept-breakdown")]
    public async Task<IActionResult> GetDeptBreakdown([FromQuery] string? department) =>
        Ok(await dashboardService.GetDeptBreakdownAsync(department));

    [HttpGet("monthly-traffic")]
    public async Task<IActionResult> GetMonthlyTraffic(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetMonthlyTrafficAsync(year, month, department, employee));

    [HttpGet("yearly-traffic")]
    public async Task<IActionResult> GetYearlyTraffic([FromQuery] int year, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetYearlyTrafficAsync(year, department, employee));

    [HttpGet("day-events")]
    public async Task<IActionResult> GetDayEvents([FromQuery] string date, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetDayEventsAsync(date, department, employee));

    [HttpGet("day-people")]
    public async Task<IActionResult> GetDayPeople([FromQuery] string date, [FromQuery] string? department, [FromQuery] string? employee) =>
        Ok(await dashboardService.GetDayPeopleAsync(date, department, employee));

}