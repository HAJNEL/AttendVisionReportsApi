using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/employees"), Authorize]
    public class EmployeesController(AppDbContext db, IEmployeeSyncService syncService, IUserService userService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<EmployeeListItem>>> GetEmployees(
            [FromQuery] Guid? departmentId,
            [FromQuery] string? search)
        {
            var query = db.Employees.Include(e => e.Department).Include(e => e.AttendanceGroup).AsQueryable();

            // Admins see every employee; everyone else only sees employees in
            // departments they're linked to via DepartmentUsers - same allow-list
            // pattern as DepartmentsService.GetAllForUserAsync.
            if (Helpers.ClaimsHelper.TryGetUserId(User, out var userId) && !await userService.IsAdminAsync(userId))
            {
                var allowedDepartmentIds = await db.DepartmentUsers
                    .Where(du => du.UserId == userId)
                    .Select(du => du.DepartmentId)
                    .ToListAsync();
                query = query.Where(e => e.DepartmentId != null && allowedDepartmentIds.Contains(e.DepartmentId.Value));
            }

            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e =>
                    (e.FullName ?? "").ToLower().Contains(term) ||
                    (e.EmployeeNo ?? "").ToLower().Contains(term));
            }

            var employees = await query.OrderBy(e => e.FullName).ToListAsync();
            var employeeIds = employees.Select(e => e.Id).ToList();
            var accessLevels = await db.EmployeeAccessLevels
                .Where(a => employeeIds.Contains(a.EmployeeId))
                .ToListAsync();

            var result = employees.Select(e => new EmployeeListItem(
                e.Id,
                e.HikCentralPersonId,
                e.EmployeeNo,
                e.FullName,
                e.FirstName,
                e.LastName,
                e.Gender,
                e.DepartmentId,
                e.Department?.DepartmentName,
                e.Position,
                e.OrgIndexCode,
                e.PhoneNo,
                e.Email,
                e.JobNo,
                e.PhotoBase64,
                e.CurrentShiftName,
                e.CurrentShiftOnDuty,
                e.CurrentShiftOffDuty,
                accessLevels
                    .Where(a => a.EmployeeId == e.Id)
                    .Select(a => a.TemplateName ?? a.TemplateId ?? "")
                    .Where(n => n != "")
                    .ToList(),
                e.BeginTime,
                e.EndTime,
                e.LastSyncedAt,
                e.AttendanceGroupId,
                e.AttendanceGroup?.Name
            )).ToList();

            return Ok(result);
        }

        // Local-only assignment, not pushed to HikCentral - AttendanceGroup is
        // a purely local grouping concept, unlike the rest of this controller's
        // writes which go through IEmployeeSyncService and call HikCentral.
        [HttpPut("{id}/attendance-group")]
        public async Task<IActionResult> SetAttendanceGroup(Guid id, [FromBody] Guid? attendanceGroupId)
        {
            var employee = await db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            if (attendanceGroupId.HasValue && !await db.AttendanceGroups.AnyAsync(g => g.Id == attendanceGroupId.Value))
                return BadRequest("Attendance group not found.");

            employee.AttendanceGroupId = attendanceGroupId;
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("sync")]
        public async Task<ActionResult<EmployeeSyncResult>> Sync(CancellationToken ct) =>
            Ok(await syncService.SyncAllAsync(ct));

        [HttpGet("{id}/hikcentral-test")]
        public async Task<ActionResult<EmployeeHikCentralTestResult>> TestHikCentralFetch(Guid id, CancellationToken ct)
        {
            try
            {
                return Ok(await syncService.TestFetchAsync(id, ct));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // Direct writes bypass the approval workflow entirely, so they stay
        // admin-only - anyone else must submit via /api/temp-employees for an
        // admin (or a holder of employees_approval) to approve.
        [HttpPost]
        public async Task<ActionResult<EmployeeListItem>> Create(EmployeeCreateRequest request, CancellationToken ct)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId) || !await userService.IsAdminAsync(userId))
                return Forbid();

            try
            {
                return Ok(await syncService.CreateAsync(request, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeListItem>> Update(Guid id, EmployeeUpdateRequest request, CancellationToken ct)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId) || !await userService.IsAdminAsync(userId))
                return Forbid();

            try
            {
                return Ok(await syncService.UpdateAsync(id, request, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
