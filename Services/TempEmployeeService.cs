using System.Security.Claims;
using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Helpers;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class TempEmployeeService(AppDbContext db, IEmployeeSyncService employeeSyncService, IUserService userService) : ITempEmployeeService
    {
        public async Task<List<TempEmployeeListItem>> GetPendingAsync(ClaimsPrincipal user, CancellationToken ct = default)
        {
            if (!ClaimsHelper.TryGetUserId(user, out var userId))
                return [];

            var query = db.TempEmployees.AsQueryable();
            if (!await userService.IsAdminAsync(userId))
            {
                var departmentIds = await GetAllowedDepartmentIdsAsync(userId, ct);
                query = query.Where(t => t.DepartmentId != null && departmentIds.Contains(t.DepartmentId.Value));
            }

            var tempEmployees = await query.ToListAsync(ct);

            var departmentIdsUsed = tempEmployees.Where(t => t.DepartmentId.HasValue).Select(t => t.DepartmentId!.Value).Distinct().ToList();
            var departmentNames = await db.Departments
                .Where(d => departmentIdsUsed.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.DepartmentName, ct);

            var submitterIds = tempEmployees.Where(t => t.SubmittedBy.HasValue).Select(t => t.SubmittedBy!.Value).Distinct().ToList();
            var submitterNames = await db.Users
                .Where(u => submitterIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username, ct);

            return tempEmployees
                .Select(t => Map(t,
                    t.DepartmentId.HasValue && departmentNames.TryGetValue(t.DepartmentId.Value, out var dn) ? dn : null,
                    t.SubmittedBy.HasValue && submitterNames.TryGetValue(t.SubmittedBy.Value, out var sn) ? sn : null))
                .ToList();
        }

        public async Task<TempEmployeeListItem> SubmitAsync(TempEmployeeSubmitRequest request, Guid submittedByUserId, CancellationToken ct = default)
        {
            if (request.EmployeeId is Guid existingId)
                await EnsureNoPendingRequestAsync(existingId, ct);

            var now = DateTime.UtcNow;
            var temp = new TempEmployee
            {
                EmployeeId = request.EmployeeId,
                RequestType = request.EmployeeId is null ? "Create" : "Update",
                HasChanges = request.EmployeeId != null,
                EmployeeNo = request.EmployeeNo,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DepartmentId = request.DepartmentId,
                Gender = request.Gender,
                PhoneNo = request.PhoneNo,
                Email = request.Email,
                JobNo = request.JobNo,
                Remark = request.Remark,
                PhotoBase64 = request.PhotoBase64,
                SubmittedBy = submittedByUserId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.TempEmployees.Add(temp);
            await db.SaveChangesAsync(ct);

            return await MapWithLookupsAsync(temp, ct);
        }

        public async Task<TempEmployeeListItem> SubmitDeleteRequestAsync(Guid employeeId, string? reason, Guid submittedByUserId, CancellationToken ct = default)
        {
            await EnsureNoPendingRequestAsync(employeeId, ct);

            var employee = await db.Employees.FindAsync([employeeId], ct)
                ?? throw new InvalidOperationException("Employee not found.");

            var now = DateTime.UtcNow;
            var temp = new TempEmployee
            {
                EmployeeId = employeeId,
                RequestType = "Delete",
                HasChanges = false,
                // Snapshotted for display on the Deletions tab, same as
                // Create/Update rows snapshot their own field values.
                EmployeeNo = employee.EmployeeNo,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DepartmentId = employee.DepartmentId,
                Gender = employee.Gender,
                PhoneNo = employee.PhoneNo,
                Email = employee.Email,
                JobNo = employee.JobNo,
                Remark = reason,
                PhotoBase64 = employee.PhotoBase64,
                SubmittedBy = submittedByUserId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.TempEmployees.Add(temp);
            await db.SaveChangesAsync(ct);

            return await MapWithLookupsAsync(temp, ct);
        }

        public async Task<TempEmployeeListItem> UpdateAsync(Guid tempId, TempEmployeeSubmitRequest request, Guid callerUserId, bool isAdmin, CancellationToken ct = default)
        {
            var temp = await db.TempEmployees.FindAsync([tempId], ct)
                ?? throw new InvalidOperationException("Pending employee record not found.");
            if (!isAdmin && temp.SubmittedBy != callerUserId)
                throw new InvalidOperationException("You can only edit your own pending submissions.");

            temp.EmployeeNo = request.EmployeeNo;
            temp.FirstName = request.FirstName;
            temp.LastName = request.LastName;
            temp.DepartmentId = request.DepartmentId;
            temp.Gender = request.Gender;
            temp.PhoneNo = request.PhoneNo;
            temp.Email = request.Email;
            temp.JobNo = request.JobNo;
            temp.Remark = request.Remark;
            temp.PhotoBase64 = request.PhotoBase64;
            temp.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return await MapWithLookupsAsync(temp, ct);
        }

        public async Task ApproveAsync(Guid tempId, Guid decidedByUserId, bool isAdmin, CancellationToken ct = default)
        {
            var temp = await db.TempEmployees.FindAsync([tempId], ct)
                ?? throw new InvalidOperationException("Pending employee record not found.");
            if (!isAdmin)
                await EnsureCallerCanActOnAsync(temp, decidedByUserId, ct);

            switch (temp.RequestType)
            {
                case "Create":
                    var createRequest = new EmployeeCreateRequest(
                        temp.EmployeeNo ?? "",
                        temp.FirstName ?? "",
                        temp.LastName ?? "",
                        temp.DepartmentId ?? throw new InvalidOperationException("This submission has no department set."),
                        temp.Gender,
                        temp.PhoneNo,
                        temp.Email,
                        temp.Remark,
                        temp.PhotoBase64
                    );
                    await employeeSyncService.CreateAsync(createRequest, ct);
                    break;

                case "Delete":
                    var deleteTargetId = temp.EmployeeId
                        ?? throw new InvalidOperationException("This delete request has no linked employee.");
                    await employeeSyncService.DeleteAsync(deleteTargetId, ct);
                    break;

                default: // "Update"
                    var updateRequest = new EmployeeUpdateRequest(
                        temp.FirstName ?? "",
                        temp.LastName ?? "",
                        temp.DepartmentId,
                        temp.Gender,
                        temp.PhoneNo,
                        temp.Email,
                        temp.JobNo,
                        temp.PhotoBase64
                    );
                    await employeeSyncService.UpdateAsync(temp.EmployeeId!.Value, updateRequest, ct);
                    break;
            }

            await LogDecisionAsync(temp, "Approved", null, decidedByUserId, ct);
            db.TempEmployees.Remove(temp);
            await db.SaveChangesAsync(ct);
        }

        public async Task RejectAsync(Guid tempId, Guid decidedByUserId, bool isAdmin, string? reason, CancellationToken ct = default)
        {
            var temp = await db.TempEmployees.FindAsync([tempId], ct)
                ?? throw new InvalidOperationException("Pending employee record not found.");
            if (!isAdmin)
                await EnsureCallerCanActOnAsync(temp, decidedByUserId, ct);

            await LogDecisionAsync(temp, "Rejected", reason, decidedByUserId, ct);
            db.TempEmployees.Remove(temp);
            await db.SaveChangesAsync(ct);
        }

        // A pending Update/Delete request already exists for this employee -
        // block a second one rather than letting two conflicting drafts
        // (e.g. an edit and a delete) race to be approved independently.
        private async Task EnsureNoPendingRequestAsync(Guid employeeId, CancellationToken ct)
        {
            if (await db.TempEmployees.AnyAsync(t => t.EmployeeId == employeeId, ct))
                throw new InvalidOperationException("This employee already has a pending request awaiting approval.");
        }

        private async Task EnsureCallerCanActOnAsync(TempEmployee temp, Guid callerUserId, CancellationToken ct)
        {
            var allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(callerUserId, ct);
            if (temp.DepartmentId is null || !allowedDepartmentIds.Contains(temp.DepartmentId.Value))
                throw new InvalidOperationException("You are not authorized to approve or reject requests outside your assigned departments.");
        }

        private async Task<List<Guid>> GetAllowedDepartmentIdsAsync(Guid userId, CancellationToken ct) =>
            await db.DepartmentUsers.Where(du => du.UserId == userId).Select(du => du.DepartmentId).ToListAsync(ct);

        private async Task LogDecisionAsync(TempEmployee temp, string decision, string? reason, Guid decidedByUserId, CancellationToken ct)
        {
            db.TempEmployeeHistories.Add(new TempEmployeeHistory
            {
                Id = Guid.NewGuid(),
                RequestType = temp.RequestType,
                EmployeeId = temp.EmployeeId,
                EmployeeNo = temp.EmployeeNo,
                FirstName = temp.FirstName,
                LastName = temp.LastName,
                DepartmentId = temp.DepartmentId,
                SubmittedBy = temp.SubmittedBy,
                DecidedBy = decidedByUserId,
                Decision = decision,
                Reason = reason ?? temp.Remark,
                // temp.CreatedAt was read back from Postgres as Kind=Unspecified
                // (the temp_employees.created_at column isn't timestamptz) -
                // Npgsql refuses to write that into temp_employee_history's
                // timestamptz-mapped column. The stored instant is already UTC
                // (always written via DateTime.UtcNow), so this only corrects
                // the Kind tag, not the value - same pattern as
                // EmployeeSyncService.ParseHikTime.
                SubmittedAt = DateTime.SpecifyKind(temp.CreatedAt, DateTimeKind.Utc),
                DecidedAt = DateTime.UtcNow,
            });
        }

        private async Task<TempEmployeeListItem> MapWithLookupsAsync(TempEmployee t, CancellationToken ct)
        {
            var departmentName = t.DepartmentId.HasValue
                ? await db.Departments.Where(d => d.Id == t.DepartmentId).Select(d => d.DepartmentName).FirstOrDefaultAsync(ct)
                : null;
            var submittedByName = t.SubmittedBy.HasValue
                ? await db.Users.Where(u => u.Id == t.SubmittedBy).Select(u => u.Username).FirstOrDefaultAsync(ct)
                : null;
            return Map(t, departmentName, submittedByName);
        }

        private static TempEmployeeListItem Map(TempEmployee t, string? departmentName, string? submittedByName) =>
            new(
                t.Id,
                t.EmployeeId,
                t.RequestType switch { "Create" => "New", "Delete" => "Delete", _ => "Change" },
                t.HasChanges,
                t.EmployeeNo,
                t.FirstName,
                t.LastName,
                t.Gender,
                t.DepartmentId,
                departmentName,
                t.PhoneNo,
                t.Email,
                t.JobNo,
                t.Remark,
                t.PhotoBase64,
                t.SubmittedBy,
                submittedByName,
                t.CreatedAt,
                t.UpdatedAt
            );
    }
}
