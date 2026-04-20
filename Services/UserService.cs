using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using AttendVisionReportsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
        public class UserService : IUserService
        {
            private readonly AppDbContext db;
            public UserService(AppDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<PermissionDto>?> GetPermissionsForCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user)
            {
                // Debug: log all claims (set a breakpoint or log as needed)
                var claims = user.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
                // Example: System.Diagnostics.Debug.WriteLine(string.Join("; ", claims));

                // Try common claim types for user id
                var userIdClaim = user.Claims.FirstOrDefault(c =>
                    c.Type == "sub" ||
                    c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" ||
                    c.Type.EndsWith("nameidentifier"));

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return null;

                var permissions = await (from u in db.Users
                                         join ur in db.UserRoles on u.Id equals ur.UserId
                                         join r in db.Roles on ur.RoleId equals r.Id
                                         join rp in db.RolePermissions on r.Id equals rp.RoleId
                                         join p in db.Permissions on rp.PermissionId equals p.Id
                                         where u.Id == userId
                                         select new PermissionDto
                                         {
                                             Id = p.Id,
                                             ParentId = p.ParentId,
                                             Name = p.Name,
                                             Description = p.Description,
                                             UniqueCode = p.UniqueCode
                                         })
                                         .Distinct()
                                         .ToListAsync();
                return permissions;
            }
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
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                IsActive = u.IsActive,
                ResetPassword = u.ResetPassword,
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
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsActive = user.IsActive,
                ResetPassword = user.ResetPassword,
                Roles = roles
            };
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = (!string.IsNullOrWhiteSpace(dto.FirstName) && !string.IsNullOrWhiteSpace(dto.LastName))
                    ? string.Concat(dto.FirstName.ToLower(), ".", dto.LastName.ToLower())
                    : dto.Email,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                ResetPassword = dto.ResetPassword
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
            if (!string.IsNullOrEmpty(dto.Password)) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.FirstName) || !string.IsNullOrEmpty(dto.LastName))
                user.Username = (!string.IsNullOrWhiteSpace(dto.FirstName) && !string.IsNullOrWhiteSpace(dto.LastName))
                    ? string.Concat(dto.FirstName.ToLower(), ".", dto.LastName.ToLower())
                    : user.Email;
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            if (dto.ResetPassword.HasValue)
                user.ResetPassword = dto.ResetPassword.Value;
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

            public async Task<IEnumerable<DepartmentResponse>> GetDepartmentsForUserAsync(Guid userId)
        {
            var departmentIds = await db.DepartmentUsers
                .Where(du => du.UserId == userId)
                .Select(du => du.DepartmentId)
                .ToListAsync();
            return await (from d in db.Departments
                          where departmentIds.Contains(d.Id)
                          join c in db.Companies on d.CompanyId equals c.Id into companyJoin
                          from c in companyJoin.DefaultIfEmpty()
                          orderby d.DepartmentName
                          select new DepartmentResponse(
                              d.Id,
                              d.DepartmentName,
                              d.Manager,
                              (double?)d.PaymentRate,
                              d.AddressLine1,
                              d.AddressLine2,
                              d.City,
                              d.State,
                              d.PostalCode,
                              d.Country,
                              d.SerialNo,
                              d.CompanyId,
                              c != null ? c.Name : null,
                              (double?)d.OvertimePaymentRate,
                              d.OvertimeStartAfterTime.HasValue ? d.OvertimeStartAfterTime.Value.ToString(@"hh\:mm\:ss") : null,
                              d.CheckInOverrideTime.HasValue ? d.CheckInOverrideTime.Value.ToString(@"hh\:mm\:ss") : null
                          )).ToListAsync();
        }

        public async Task<IEnumerable<UserDto>> GetUsersForDepartmentAsync(Guid departmentId)
        {
            var userIds = await db.DepartmentUsers
                .Where(du => du.DepartmentId == departmentId)
                .Select(du => du.UserId)
                .ToListAsync();
            var users = await db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.Username.Contains(".") ? u.Username.Substring(0, u.Username.IndexOf(".")) : u.Username,
                LastName = u.Username.Contains(".") ? u.Username.Substring(u.Username.IndexOf(".") + 1) : "",
                IsActive = u.IsActive,
                Roles = new List<RoleDto>() // Optionally populate roles if needed
            });
        }
    }
}