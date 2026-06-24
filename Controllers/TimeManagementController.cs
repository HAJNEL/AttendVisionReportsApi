using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController]
    [Route("api/time-management")]
    [Authorize]
    public class TimeManagementController(ITimeManagementService service) : ControllerBase
    {
        [HttpGet("day-summary")]
        public async Task<IActionResult> GetDaySummary(
            [FromQuery] string date,
            [FromQuery] Guid? departmentId,
            [FromQuery] string? employeeId)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized(new { error = "No valid user ID found in claims." });
            try
            {
                return Ok(await service.GetDaySummaryAsync(date, departmentId, employeeId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user-records")]
        public async Task<IActionResult> GetUserRecords(
            [FromQuery] string date,
            [FromQuery] string employeeId)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized(new { error = "No valid user ID found in claims." });
            try
            {
                return Ok(await service.GetUserRecordsAsync(date, employeeId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user-issues")]
        public async Task<IActionResult> GetUserIssues(
            [FromQuery] string date,
            [FromQuery] string employeeId)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized(new { error = "No valid user ID found in claims." });
            try
            {
                return Ok(await service.GetUserIssuesAsync(date, employeeId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("access-record")]
        public async Task<IActionResult> CreateAccessRecord([FromBody] CreateAccessRecordDto dto)
        {
            try
            {
                return Ok(await service.CreateAccessRecordAsync(dto));
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = detail });
            }
        }

        [HttpPut("access-record/{id:long}")]
        public async Task<IActionResult> UpdateAccessRecord(long id, [FromBody] UpdateAccessRecordDto dto)
        {
            try
            {
                return Ok(await service.UpdateAccessRecordAsync(id, dto.Time, dto.AttendanceStatus));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { error = detail });
            }
        }

        [HttpDelete("access-record/{id:long}")]
        public async Task<IActionResult> DeleteAccessRecord(long id)
        {
            try
            {
                await service.DeleteAccessRecordAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("auto-fix-preview")]
        public async Task<IActionResult> GetAutoFixPreview(
            [FromQuery] string date,
            [FromQuery] string employeeId)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized(new { error = "No valid user ID found in claims." });
            try
            {
                return Ok(await service.GetAutoFixPreviewAsync(date, employeeId, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("auto-fix-apply")]
        public async Task<IActionResult> ApplyAutoFix([FromBody] AutoFixApplyRequest request)
        {
            try
            {
                await service.ApplyAutoFixAsync(request);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("config")]
        public async Task<IActionResult> GetConfig()
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized(new { error = "No valid user ID found in claims." });
            try
            {
                return Ok(await service.GetConfigAsync(userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("config")]
        public async Task<IActionResult> SaveConfig([FromBody] TimeManagementConfigDto dto)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized(new { error = "No valid user ID found in claims." });
            try
            {
                return Ok(await service.SaveConfigAsync(dto, userId));
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
