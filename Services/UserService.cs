using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using AttendVisionReportsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class UserService(AppDbContext db) : IUserService
    {
        public async Task<IEnumerable<UserDto>> GetAllAsync() =>
            await db.Users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive
            }).ToListAsync();

        public async Task<UserDto?> GetByIdAsync(Guid id) =>
            await db.Users.Where(u => u.Id == id).Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive
            }).FirstOrDefaultAsync();

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = dto.Password, // Hash in real app!
                FullName = dto.FullName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return await GetByIdAsync(user.Id) ?? throw new Exception("User creation failed");
        }

        public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto)
        {
            var user = await db.Users.FindAsync(id);
            if (user == null) return null;
            if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Password)) user.PasswordHash = dto.Password; // Hash in real app!
            if (!string.IsNullOrEmpty(dto.FullName)) user.FullName = dto.FullName;
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            await db.SaveChangesAsync();
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
