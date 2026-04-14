
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using AttendVisionReportsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class UserService(AppDbContext db) : IUserService
    {
        public async Task<IEnumerable<PermissionDto>> GetPermissionsForUserAsync(Guid userId)
        {
            // Get all role ids for the user
            var roleIds = await db.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            // Get all permissions for those roles
            var permissions = await db.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Include(rp => rp.Permission)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    ParentId = rp.Permission.ParentId,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    UniqueCode = rp.Permission.UniqueCode
                })
                .ToListAsync();

            // Deduplicate by UniqueCode (case-insensitive) or Id
            var unique = permissions
                .GroupBy(p => string.IsNullOrEmpty(p.UniqueCode) ? p.Id.ToString() : p.UniqueCode.ToLower())
                .Select(g => g.First())
                .ToList();
            return unique;
        }
        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await db.Users.ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();
            var userRoles = await db.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Include(ur => ur.Role)
                .ToListAsync();

            var result = users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.Username.Contains(".") ? u.Username.Substring(0, u.Username.IndexOf(".")) : u.Username,
                LastName = u.Username.Contains(".") ? u.Username.Substring(u.Username.IndexOf(".") + 1) : "",
                IsActive = u.IsActive,
                Roles = userRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Select(ur => new RoleDto
                    {
                        Id = ur.Role.Id,
                        Name = ur.Role.Name,
                        Description = ur.Role.Description
                    }).ToList()
            });
            return result;
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;

            var roles = await db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name,
                    Description = ur.Role.Description
                }).ToListAsync();

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.Username.Contains(".") ? user.Username.Substring(0, user.Username.IndexOf(".")) : user.Username,
                LastName = user.Username.Contains(".") ? user.Username.Substring(user.Username.IndexOf(".") + 1) : "",
                IsActive = user.IsActive,
                Roles = roles
            };
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = string.Concat(dto.FirstName, ".", dto.LastName),
                Email = dto.Email,
                PasswordHash = dto.Password,
                FullName = string.Concat(dto.FirstName, dto.LastName),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Assign roles
            if (dto.Roles != null && dto.Roles.Count > 0)
            {
                foreach (var roleId in dto.Roles)
                {
                    db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = user.Id, RoleId = roleId });
                }
                await db.SaveChangesAsync();
            }

            return await GetByIdAsync(user.Id) ?? throw new Exception("User creation failed");
        }

        public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto)
        {
            var user = await db.Users.FindAsync(id);
            if (user == null) return null;
            if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Password)) user.PasswordHash = dto.Password; // Hash in real app!
            if (!string.IsNullOrEmpty(dto.FirstName) || !string.IsNullOrEmpty(dto.LastName)) user.FullName = string.Concat(dto.FirstName, " ", dto.LastName);
            if (!string.IsNullOrEmpty(dto.FirstName) || !string.IsNullOrEmpty(dto.LastName)) user.Username = string.Concat(dto.FirstName, ".", dto.LastName);
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            await db.SaveChangesAsync();

            // Update roles
            if (dto.Roles != null)
            {
                var existingRoles = db.UserRoles.Where(ur => ur.UserId == user.Id);
                db.UserRoles.RemoveRange(existingRoles);
                await db.SaveChangesAsync();
                foreach (var roleId in dto.Roles)
                {
                    db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = user.Id, RoleId = roleId });
                }
                await db.SaveChangesAsync();
            }

            return await GetByIdAsync(user.Id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await db.Users.FindAsync(id);
            if (user == null) return false;
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return true;
        }
    }
}
