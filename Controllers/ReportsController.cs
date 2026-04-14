using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController(IReportsService reportsService) : ControllerBase
    {
        [HttpGet("issues")]
        public async Task<IActionResult> GetIssues(
            [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] string? department) =>
            Ok(await reportsService.GetIssuesAsync(dateFrom, dateTo, department));

        [HttpGet("clockings")]
        public async Task<IActionResult> GetClockings(
            [FromQuery] string dateFrom, [FromQuery] string dateTo,
            [FromQuery] string? dept, [FromQuery] string? user) =>
            Ok(await reportsService.GetClockingsAsync(dateFrom, dateTo, dept, user));

        [HttpGet("timesheet/users")]
        public async Task<IActionResult> GetTimesheetUsers(
            [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] string? dept) =>
            Ok(await reportsService.GetTimesheetUsersAsync(dateFrom, dateTo, dept));

        [HttpGet("timesheet")]
        public async Task<IActionResult> GetTimesheet(
            [FromQuery] string dateFrom, [FromQuery] string dateTo,
            [FromQuery] string? dept, [FromQuery] string? user) =>
            Ok(await reportsService.GetTimesheetAsync(dateFrom, dateTo, dept, user));
    }
}
