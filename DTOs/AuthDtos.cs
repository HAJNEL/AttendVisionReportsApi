namespace AttendVisionReportsApi.DTOs
{
    public record LoginRequest(string Username, string Password);
    public record RegisterRequest(string Username, string Email, string Password, string? FullName);
    public record LoginResponse(Guid Id, string Username, string Email, string? FullName);
}
