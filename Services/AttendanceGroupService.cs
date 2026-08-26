using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using AttendVisionReportsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class AttendanceGroupService(AppDbContext db) : IAttendanceGroupService
    {
        public async Task<IEnumerable<AttendanceGroupDto>> GetAllAsync() =>
            await db.AttendanceGroups
                .OrderBy(g => g.Name)
                .Select(g => new AttendanceGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                }).ToListAsync();

        public async Task<AttendanceGroupDto?> GetByIdAsync(Guid id) =>
            await db.AttendanceGroups.Where(g => g.Id == id).Select(g => new AttendanceGroupDto
            {
                Id = g.Id,
                Name = g.Name,
            }).FirstOrDefaultAsync();

        public async Task<AttendanceGroupDto> CreateAsync(CreateAttendanceGroupDto dto)
        {
            var group = new AttendanceGroup
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
            };
            db.AttendanceGroups.Add(group);
            await db.SaveChangesAsync();
            return await GetByIdAsync(group.Id) ?? throw new Exception("Attendance group creation failed");
        }

        public async Task<AttendanceGroupDto?> UpdateAsync(Guid id, UpdateAttendanceGroupDto dto)
        {
            var group = await db.AttendanceGroups.FindAsync(id);
            if (group == null) return null;
            if (!string.IsNullOrEmpty(dto.Name)) group.Name = dto.Name;
            await db.SaveChangesAsync();
            return await GetByIdAsync(group.Id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var group = await db.AttendanceGroups.FindAsync(id);
            if (group == null) return false;
            db.AttendanceGroups.Remove(group);
            await db.SaveChangesAsync();
            return true;
        }

        // Replaces the group's full membership in one call - local-only, not
        // pushed to HikCentral (see EmployeesController.SetAttendanceGroup).
        public async Task<bool> SetMembersAsync(Guid groupId, List<Guid> employeeIds)
        {
            if (!await db.AttendanceGroups.AnyAsync(g => g.Id == groupId)) return false;

            var currentMembers = await db.Employees.Where(e => e.AttendanceGroupId == groupId).ToListAsync();
            foreach (var emp in currentMembers.Where(e => !employeeIds.Contains(e.Id)))
                emp.AttendanceGroupId = null;

            var toAdd = await db.Employees
                .Where(e => employeeIds.Contains(e.Id) && e.AttendanceGroupId != groupId)
                .ToListAsync();
            foreach (var emp in toAdd)
                emp.AttendanceGroupId = groupId;

            await db.SaveChangesAsync();
            return true;
        }
    }
}
