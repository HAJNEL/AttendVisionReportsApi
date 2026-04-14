using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _db;
        public PermissionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<PermissionDto>> GetAllAsync()
        {
            return await _db.Permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                ParentId = p.ParentId,
                Name = p.Name,
                Description = p.Description,
                UniqueCode = p.UniqueCode
            }).ToListAsync();
        }

        public async Task<PermissionDto?> GetByIdAsync(Guid id)
        {
            var p = await _db.Permissions.FindAsync(id);
            if (p == null) return null;
            return new PermissionDto
            {
                Id = p.Id,
                ParentId = p.ParentId,
                Name = p.Name,
                Description = p.Description,
                UniqueCode = p.UniqueCode
            };
        }

        public async Task<PermissionDto> CreateAsync(CreatePermissionDto dto)
        {
            var p = new Permission
            {
                ParentId = dto.ParentId,
                Name = dto.Name,
                Description = dto.Description,
                UniqueCode = dto.UniqueCode
            };
            _db.Permissions.Add(p);
            await _db.SaveChangesAsync();
            return new PermissionDto
            {
                Id = p.Id,
                ParentId = p.ParentId,
                Name = p.Name,
                Description = p.Description,
                UniqueCode = p.UniqueCode
            };
        }

        public async Task<PermissionDto?> UpdateAsync(Guid id, UpdatePermissionDto dto)
        {
            var p = await _db.Permissions.FindAsync(id);
            if (p == null) return null;
            p.ParentId = dto.ParentId;
            p.Name = dto.Name;
            p.Description = dto.Description;
            p.UniqueCode = dto.UniqueCode;
            await _db.SaveChangesAsync();
            return new PermissionDto
            {
                Id = p.Id,
                ParentId = p.ParentId,
                Name = p.Name,
                Description = p.Description,
                UniqueCode = p.UniqueCode
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var p = await _db.Permissions.FindAsync(id);
            if (p == null) return false;
            _db.Permissions.Remove(p);
            await _db.SaveChangesAsync();
            return true;
        }


        public async Task<bool> AssignPermissionsAsync(Guid roleId, List<Guid> permissionIds)
        {
            // Get current permissions for the role
            var current = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            var currentIds = current.Select(rp => rp.PermissionId).ToHashSet();
            var newIds = permissionIds.ToHashSet();

            // Permissions to remove
            var toRemove = current.Where(rp => !newIds.Contains(rp.PermissionId)).ToList();
            // Permissions to add
            var toAdd = newIds.Except(currentIds).ToList();

            if (!toRemove.Any() && !toAdd.Any())
                return false; // No changes

            if (toRemove.Any())
                _db.RolePermissions.RemoveRange(toRemove);
            foreach (var pid in toAdd)
                _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemovePermissionAsync(Guid roleId, Guid permissionId)
        {
            var rp = await _db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (rp == null) return false;
            _db.RolePermissions.Remove(rp);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PermissionDto>> GetPermissionsByRoleAsync(Guid roleId)
        {
            return await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    ParentId = rp.Permission.ParentId,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    UniqueCode = rp.Permission.UniqueCode
                }).ToListAsync();
        }
    }
}
