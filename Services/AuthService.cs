using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class AuthService(AppDbContext db) : IAuthService
    {
        public Task<bool> UsersExistAsync() => db.Users.AnyAsync();

        public async Task<(LoginResponse? Response, string? Error)> RegisterAsync(RegisterRequest req)
        {
            if (await db.Users.AnyAsync())
                return (null, "Registration is disabled. Users already exist.");

            var user = new User
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                FullName = req.FullName
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return (Map(user), null);
        }

        public async Task<(LoginResponse? Response, string? Error)> LoginAsync(LoginRequest req)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return (null, "Invalid username or password");

            if (!user.IsActive)
                return (null, "Account is disabled.");

            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return (Map(user), null);
        }

        private static LoginResponse Map(User u) =>
            new(u.Id, u.Username, u.Email, u.FullName);
    }
}
