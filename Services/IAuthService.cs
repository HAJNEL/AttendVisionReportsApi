using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IAuthService
    {
        Task<bool> UsersExistAsync();
        Task<(LoginResponse? Response, string? Error)> RegisterAsync(RegisterRequest req);
        Task<(LoginResponse? Response, string? Error)> LoginAsync(LoginRequest req);
    }
}
