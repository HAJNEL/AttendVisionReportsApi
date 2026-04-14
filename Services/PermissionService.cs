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

        public async Task<bool> AssignPermissionAsync(Guid roleId, Guid permissionId)
        {
            var exists = await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (exists) return false;
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
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
