using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using AttendVisionReportsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class RoleService(AppDbContext db) : IRoleService
    {
        public async Task<IEnumerable<RoleDto>> GetAllAsync() =>
            await db.Roles.Select(r => new RoleDto
            {
                Id = r.Id,
                ParentId = r.ParentId,
                Name = r.Name,
                Description = r.Description,
                UniqueCode = r.UniqueCode
            }).ToListAsync();

        public async Task<RoleDto?> GetByIdAsync(Guid id) =>
            await db.Roles.Where(r => r.Id == id).Select(r => new RoleDto
            {
                Id = r.Id,
                ParentId = r.ParentId,
                Name = r.Name,
                Description = r.Description,
                UniqueCode = r.UniqueCode
            }).FirstOrDefaultAsync();

        public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
        {
            var role = new Role
            {
                Id = Guid.NewGuid(),
                ParentId = dto.ParentId,
                Name = dto.Name,
                Description = dto.Description,
                UniqueCode = dto.UniqueCode
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            return await GetByIdAsync(role.Id) ?? throw new Exception("Role creation failed");
        }

        public async Task<RoleDto?> UpdateAsync(Guid id, UpdateRoleDto dto)
        {
            var role = await db.Roles.FindAsync(id);
            if (role == null) return null;
            if (dto.ParentId.HasValue) role.ParentId = dto.ParentId;
            if (!string.IsNullOrEmpty(dto.Name)) role.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Description)) role.Description = dto.Description;
            if (!string.IsNullOrEmpty(dto.UniqueCode)) role.UniqueCode = dto.UniqueCode;
            await db.SaveChangesAsync();
            return await GetByIdAsync(role.Id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var role = await db.Roles.FindAsync(id);
            if (role == null) return false;
            db.Roles.Remove(role);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId)
        {
            var exists = await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (exists) return false;
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId, AssignedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleAsync(Guid userId, Guid roleId)
        {
            var ur = await db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (ur == null) return false;
            db.UserRoles.Remove(ur);
            await db.SaveChangesAsync();
            return true;
        }
    }
}
