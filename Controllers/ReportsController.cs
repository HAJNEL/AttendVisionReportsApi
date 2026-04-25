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
            [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] string? department, [FromQuery] string? employeeId)
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.GetIssuesAsync(dateFrom, dateTo, department, employeeId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("clockings")]
        public async Task<IActionResult> GetClockings(
            [FromQuery] string dateFrom, [FromQuery] string dateTo,
            [FromQuery] string? dept, [FromQuery] string? employeeId)
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.GetClockingsAsync(dateFrom, dateTo, dept, employeeId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("timesheet")]
        public async Task<IActionResult> GetTimesheet(
            [FromQuery] string dateFrom, [FromQuery] string dateTo,
            [FromQuery] string? dept, [FromQuery] string? employeeId)
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.GetTimesheetAsync(dateFrom, dateTo, dept, employeeId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
