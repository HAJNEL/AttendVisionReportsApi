using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IAuthService
    {
        Task<bool> UsersExistAsync();
        Task<(LoginResponse? Response, string? Error, string? Token)> RegisterAsync(RegisterRequest req);
        Task<(LoginResponse? Response, string? Error, string? Token)> LoginAsync(LoginRequest req);
        Task<bool> UpdatePasswordAsync(Guid userId, string newPassword);
        Task<(ImpersonationResponse? Response, string? Error)> ImpersonateAsync(Guid adminUserId, Guid targetUserId, string adminPassword);
        Task<(ExitImpersonationResponse? Response, string? Error)> ExitImpersonationAsync(Guid adminUserId);
    }
}
