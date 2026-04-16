using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AttendVisionReportsApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            this.db = db;
            _config = config;
        }

        public Task<bool> UsersExistAsync() => db.Users.AnyAsync();

        public async Task<(LoginResponse? Response, string? Error, string? Token)> RegisterAsync(RegisterRequest req)
        {
            if (await db.Users.AnyAsync())
                return (null, "Registration is disabled. Users already exist.", null);

            string? firstName = null, lastName = null;
            if (!string.IsNullOrWhiteSpace(req.FullName))
            {
                var parts = req.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                firstName = parts.Length > 0 ? parts[0] : null;
                lastName = parts.Length > 1 ? parts[1] : null;
            }
            var user = new User
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                FirstName = firstName,
                LastName = lastName
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            var token = GenerateJwtToken(user);
            return (Map(user), null, token);
        }

        public async Task<(LoginResponse? Response, string? Error, string? Token)> LoginAsync(LoginRequest req)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user is null)
                return (null, "Invalid username or password", null);

            // Check if the hash is a valid BCrypt hash
            if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !(user.PasswordHash.StartsWith("$2a$") || user.PasswordHash.StartsWith("$2b$") || user.PasswordHash.StartsWith("$2y$")))
            {
                return (null, "Your password needs to be reset due to a legacy or invalid password format.", null);
            }

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return (null, "Invalid username or password", null);

            if (!user.IsActive)
                return (null, "Account is disabled.", null);

            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            var token = GenerateJwtToken(user);
            return (Map(user), null, token);
        }

        private static LoginResponse Map(User u) =>
            new(u.Id, u.Username, u.Email, string.Join(" ", new[]{u.FirstName, u.LastName}.Where(x => !string.IsNullOrWhiteSpace(x))));

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
