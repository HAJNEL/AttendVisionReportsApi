using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/employees"), Authorize]
    public class EmployeesController(AppDbContext db, IEmployeeSyncService syncService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<EmployeeListItem>>> GetEmployees(
            [FromQuery] Guid? departmentId,
            [FromQuery] string? search)
        {
            var query = db.Employees.Include(e => e.Department).AsQueryable();
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
                e.DepartmentId,
                e.Department?.DepartmentName,
                e.Position,
                e.OrgIndexCode,
                e.PhoneNo,
                e.Email,
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
                e.LastSyncedAt
            )).ToList();

            return Ok(result);
        }

        [HttpPost("sync")]
        public async Task<ActionResult<EmployeeSyncResult>> Sync(CancellationToken ct) =>
            Ok(await syncService.SyncAllAsync(ct));
    }
}
