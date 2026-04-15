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
        public async Task<ActionResult<object>> Register(RegisterRequest req)
        {
            var (response, error, token) = await authService.RegisterAsync(req);
            if (response is null) return BadRequest(error);
            return Ok(new { token, user = response });
        }

        [HttpPost("login")]
        public async Task<ActionResult<object>> Login(LoginRequest req)
        {
            var (response, error, token) = await authService.LoginAsync(req);
            if (response is null) return Unauthorized(error);
            return Ok(new { token, user = response });
        }
    }
}
