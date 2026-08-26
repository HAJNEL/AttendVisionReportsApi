using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using AttendVisionReportsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class RoleService(AppDbContext db) : IRoleService
    {
        public async Task<IEnumerable<RoleDto>> GetAllAsync()
        {
            var roles = await db.Roles.ToListAsync();
            var permissionIdsByRole = await db.RolePermissions
                .GroupBy(rp => rp.RoleId)
                .Select(g => new { RoleId = g.Key, PermissionIds = g.Select(rp => rp.PermissionId).ToList() })
                .ToDictionaryAsync(g => g.RoleId, g => g.PermissionIds);

            return roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                PermissionIds = permissionIdsByRole.TryGetValue(r.Id, out var ids) ? ids : new List<Guid>(),
            });
        }

        public async Task<RoleDto?> GetByIdAsync(Guid id)
        {
            var role = await db.Roles.FindAsync(id);
            if (role == null) return null;

            var permissionIds = await db.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                PermissionIds = permissionIds,
            };
        }

        public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
        {
            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            return await GetByIdAsync(role.Id) ?? throw new Exception("Role creation failed");
        }

        public async Task<RoleDto?> UpdateAsync(Guid id, UpdateRoleDto dto)
        {
            var role = await db.Roles.FindAsync(id);
            if (role == null) return null;
            if (!string.IsNullOrEmpty(dto.Name)) role.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Description)) role.Description = dto.Description;
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


        public async Task<bool> AssignPermissionAsync(Guid roleId, Guid permissionId)
        {
            var exists = await db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (exists) return false;
            db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), RoleId = roleId, PermissionId = permissionId });
            await db.SaveChangesAsync();
            return true;
        }


        public async Task<bool> RemovePermissionAsync(Guid roleId, Guid permissionId)
        {
            var rp = await db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (rp == null) return false;
            db.RolePermissions.Remove(rp);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PermissionDto>> GetPermissionsAsync(Guid roleId)
        {
            return await db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    ParentId = rp.Permission.ParentId,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description
                }).ToListAsync();
        }


        public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId)
        {
            var exists = await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (exists) return false;
            db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleId = roleId, AssignedAt = DateTime.UtcNow });
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
