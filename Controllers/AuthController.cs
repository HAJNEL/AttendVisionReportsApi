using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }

        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest req)
        {

            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId, logClaims: false))
                return Unauthorized();

            var result = await authService.UpdatePasswordAsync(userId, req.NewPassword);
            if (!result)
                return BadRequest("Failed to update password.");
            return Ok();
        }
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
            return Ok(new {
                token,
                user = response,
                resetPassword = response.ResetPassword
            });
        }

        [Authorize]
        [HttpPost("impersonate")]
        public async Task<ActionResult<ImpersonationResponse>> Impersonate([FromBody] ImpersonateRequest req)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var adminId, logClaims: false))
                return Unauthorized();

            // Can't impersonate while already impersonating - the caller's
            // own token would have to carry this claim, which only a
            // signed impersonation token issued by this same endpoint has.
            if (User.Claims.Any(c => c.Type == "act_as_admin_id"))
                return BadRequest("You're already logged in as another user. Exit that session first.");

            var (response, error) = await authService.ImpersonateAsync(adminId, req.TargetUserId, req.AdminPassword);
            if (response is null) return BadRequest(error);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("impersonate/exit")]
        public async Task<ActionResult<ExitImpersonationResponse>> ExitImpersonation()
        {
            var actAsAdminIdClaim = User.Claims.FirstOrDefault(c => c.Type == "act_as_admin_id")?.Value;
            if (actAsAdminIdClaim == null || !Guid.TryParse(actAsAdminIdClaim, out var adminId))
                return BadRequest("You're not currently logged in as another user.");

            var (response, error) = await authService.ExitImpersonationAsync(adminId);
            if (response is null) return BadRequest(error);
            return Ok(response);
        }
    }
}
