using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    // Protect all endpoints in this controller with JWT authentication
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll() => Ok(await _userService.GetAllAsync());

        [HttpGet("by-company/{companyId}")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByCompany(Guid companyId) =>
            Ok(await _userService.GetUsersForCompanyAsync(companyId));

        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetMe()
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId, logClaims: false))
                return Unauthorized();
            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("me/photo")]
        public async Task<ActionResult<UserDto>> UpdateMyPhoto([FromBody] UpdatePhotoRequest req)
        {
            if (!Helpers.ClaimsHelper.TryGetUserId(User, out var userId, logClaims: false))
                return Unauthorized();
            var user = await _userService.UpdatePhotoAsync(userId, req.PhotoBase64);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            var user = await _userService.UpdateAsync(id, dto);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _userService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetMyPermissions()
        {
            // Debug: log all claims for the current user
            foreach (var claim in User.Claims)
            {
                System.Diagnostics.Debug.WriteLine($"CLAIM: {claim.Type} = {claim.Value}");
            }

            var result = await _userService.GetPermissionsForCurrentUserAsync(User);
            if (result == null)
                return Unauthorized();
            return Ok(result);
        }
    }
}
