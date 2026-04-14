using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpGet("users-exist")]
        public Task<bool> UsersExist() => authService.UsersExistAsync();

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register(RegisterRequest req)
        {
            var (response, error) = await authService.RegisterAsync(req);
            if (response is null) return BadRequest(error);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest req)
        {
            var (response, error) = await authService.LoginAsync(req);
            if (response is null) return Unauthorized(error);
            return Ok(response);
        }
    }
}
