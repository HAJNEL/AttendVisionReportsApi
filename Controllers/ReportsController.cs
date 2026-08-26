using AttendVisionReportsApi.DTOs;
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
            [FromQuery] string dateFrom, [FromQuery] string dateTo, [FromQuery] string? department, [FromQuery] string? employeeId, [FromQuery] Guid? attendanceGroupId)
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.GetIssuesAsync(dateFrom, dateTo, department, employeeId, attendanceGroupId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("clockings")]
        public async Task<IActionResult> GetClockings(
            [FromQuery] string dateFrom, [FromQuery] string dateTo,
            [FromQuery] string? dept, [FromQuery] string? employeeId, [FromQuery] Guid? attendanceGroupId)
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.GetClockingsAsync(dateFrom, dateTo, dept, employeeId, attendanceGroupId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("timesheet")]
        public async Task<IActionResult> GetTimesheet(
            [FromQuery] string dateFrom, [FromQuery] string dateTo,
            [FromQuery] string? dept, [FromQuery] string? employeeId, [FromQuery] Guid? attendanceGroupId)
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.GetTimesheetAsync(dateFrom, dateTo, dept, employeeId, attendanceGroupId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("sage-timesheet")]
        public async Task<IActionResult> GetSageTimesheet(
            [FromQuery] string dateFrom, [FromQuery] string dateTo,
            [FromQuery] string? dept, [FromQuery] string? employeeId, [FromQuery] Guid? attendanceGroupId)
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.GetSageTimesheetAsync(dateFrom, dateTo, dept, employeeId, attendanceGroupId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("config")]
        public async Task<IActionResult> GetReportConfig()
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.GetReportConfigAsync(userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("config")]
        public async Task<IActionResult> SaveReportConfig([FromBody] ReportConfigDto dto)
        {
            try
            {
                Guid userId;
                if (!Helpers.ClaimsHelper.TryGetUserId(User, out userId))
                {
                    return Unauthorized(new { error = "No valid user ID found in claims." });
                }
                return Ok(await reportsService.SaveReportConfigAsync(dto, userId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
