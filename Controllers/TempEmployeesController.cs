using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/temp-employees"), Authorize]
    public class TempEmployeesController(ITempEmployeeService tempEmployeeService, IUserService userService) : ControllerBase
    {
        [HttpGet]
        public Task<List<TempEmployeeListItem>> GetPending(CancellationToken ct) =>
            tempEmployeeService.GetPendingAsync(User, ct);

        [HttpPost]
        public async Task<ActionResult<TempEmployeeListItem>> Submit(TempEmployeeSubmitRequest request, CancellationToken ct)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized();

            var permissionCode = request.EmployeeId is null ? "employees_add" : "employees_edit";
            if (!await userService.IsAdminAsync(userId) && !await userService.HasPermissionAsync(userId, permissionCode))
                return Forbid();

            try
            {
                return Ok(await tempEmployeeService.SubmitAsync(request, userId, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{employeeId}/delete-request")]
        public async Task<ActionResult<TempEmployeeListItem>> SubmitDeleteRequest(Guid employeeId, TempEmployeeDeleteRequest request, CancellationToken ct)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized();
            if (!await userService.IsAdminAsync(userId) && !await userService.HasPermissionAsync(userId, "employees_delete"))
                return Forbid();

            try
            {
                return Ok(await tempEmployeeService.SubmitDeleteRequestAsync(employeeId, request.Reason, userId, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TempEmployeeListItem>> Update(Guid id, TempEmployeeSubmitRequest request, CancellationToken ct)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized();

            try
            {
                var isAdmin = await userService.IsAdminAsync(userId);
                return Ok(await tempEmployeeService.UpdateAsync(id, request, userId, isAdmin, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized();

            var isAdmin = await userService.IsAdminAsync(userId);
            if (!isAdmin && !await userService.HasPermissionAsync(userId, "employees_approval"))
                return Forbid();

            try
            {
                await tempEmployeeService.ApproveAsync(id, userId, isAdmin, ct);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Reject(Guid id, [FromQuery] string? reason, CancellationToken ct)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId))
                return Unauthorized();

            var isAdmin = await userService.IsAdminAsync(userId);
            if (!isAdmin && !await userService.HasPermissionAsync(userId, "employees_approval"))
                return Forbid();

            try
            {
                await tempEmployeeService.RejectAsync(id, userId, isAdmin, reason, ct);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
