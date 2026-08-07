namespace AttendVisionReportsApi.DTOs
{
    public record UpdatePasswordRequest(string NewPassword);
    public record LoginRequest(string Username, string Password);
    public record RegisterRequest(string Username, string Email, string Password, string? FullName);
    public record LoginResponse(Guid Id, string Username, string Email, string? FullName, bool ResetPassword);

    public record ImpersonateRequest(Guid TargetUserId, string AdminPassword);
    public record ImpersonationResponse(string Token, LoginResponse User, Guid AdminId, string AdminName);
    public record ExitImpersonationResponse(string Token, LoginResponse User);
}
