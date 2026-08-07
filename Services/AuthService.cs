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
        private readonly IUserService _userService;

        public AuthService(AppDbContext db, IConfiguration config, IUserService userService)
        {
            this.db = db;
            _config = config;
            _userService = userService;
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
            new(
                u.Id,
                u.Username,
                u.Email,
                string.Join(" ", new[]{u.FirstName, u.LastName}.Where(x => !string.IsNullOrWhiteSpace(x))),
                u.ResetPassword
            );

        private string GenerateJwtToken(User user, IEnumerable<Claim>? extraClaims = null, TimeSpan? lifetime = null)
        {
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? "")
            };
            if (extraClaims != null) claims.AddRange(extraClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromDays(7)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<bool> UpdatePasswordAsync(Guid userId, string newPassword)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPassword = false;
            await db.SaveChangesAsync();
            return true;
        }

        private const string ActAsAdminIdClaimType = "act_as_admin_id";

        // Impersonation tokens are short-lived (vs. 7 days for a real
        // login) since they're meant for a bounded support/debugging
        // session, not a standing credential.
        private static readonly TimeSpan ImpersonationTokenLifetime = TimeSpan.FromHours(2);

        public async Task<(ImpersonationResponse? Response, string? Error)> ImpersonateAsync(Guid adminUserId, Guid targetUserId, string adminPassword)
        {
            if (adminUserId == targetUserId)
                return (null, "You cannot log in as yourself.");

            var admin = await db.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (admin is null || !admin.IsActive)
                return (null, "Your account could not be verified.");

            // Re-authenticate with the ADMIN's own password, never the
            // target's - this is the "prove it's really you" gate before
            // handing out someone else's session.
            if (string.IsNullOrWhiteSpace(admin.PasswordHash) ||
                !(admin.PasswordHash.StartsWith("$2a$") || admin.PasswordHash.StartsWith("$2b$") || admin.PasswordHash.StartsWith("$2y$")) ||
                !BCrypt.Net.BCrypt.Verify(adminPassword, admin.PasswordHash))
                return (null, "Incorrect password.");

            if (!await _userService.IsAdminAsync(adminUserId))
                return (null, "Only administrators can log in as another user.");

            var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (target is null || !target.IsActive)
                return (null, "That user could not be found or is inactive.");

            // Admin-to-admin impersonation is blocked outright - this
            // feature exists for support/debugging on regular accounts, not
            // for one admin to quietly assume another's identity.
            if (await _userService.IsAdminAsync(targetUserId))
                return (null, "You cannot log in as another administrator.");

            var extraClaims = new[]
            {
                new Claim(ActAsAdminIdClaimType, admin.Id.ToString()),
            };
            var token = GenerateJwtToken(target, extraClaims, ImpersonationTokenLifetime);

            db.ImpersonationEvents.Add(new ImpersonationEvent
            {
                Id = Guid.NewGuid(),
                AdminUserId = admin.Id,
                TargetUserId = target.Id,
                StartedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            return (new ImpersonationResponse(token, Map(target), admin.Id, DisplayName(admin)), null);
        }

        public async Task<(ExitImpersonationResponse? Response, string? Error)> ExitImpersonationAsync(Guid adminUserId)
        {
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (admin is null || !admin.IsActive)
                return (null, "Your admin account could not be verified.");

            var openEvent = await db.ImpersonationEvents
                .Where(e => e.AdminUserId == adminUserId && e.EndedAt == null)
                .OrderByDescending(e => e.StartedAt)
                .FirstOrDefaultAsync();
            if (openEvent != null)
            {
                openEvent.EndedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            var token = GenerateJwtToken(admin);
            return (new ExitImpersonationResponse(token, Map(admin)), null);
        }

        private static string DisplayName(User u)
        {
            var full = string.Join(" ", new[] { u.FirstName, u.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return string.IsNullOrWhiteSpace(full) ? u.Username : full;
        }
    }
}
