using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAll() => Ok(await _permissionService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<PermissionDto>> GetById(Guid id)
        {
            var permission = await _permissionService.GetByIdAsync(id);
            if (permission == null) return NotFound();
            return Ok(permission);
        }

        [HttpPost]
        public async Task<ActionResult<PermissionDto>> Create([FromBody] CreatePermissionDto dto)
        {
            var permission = await _permissionService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = permission.Id }, permission);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PermissionDto>> Update(Guid id, [FromBody] UpdatePermissionDto dto)
        {
            var permission = await _permissionService.UpdateAsync(id, dto);
            if (permission == null) return NotFound();
            return Ok(permission);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _permissionService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionDto dto)
        {
            var assigned = await _permissionService.AssignPermissionAsync(dto.RoleId, dto.PermissionId);
            if (!assigned) return BadRequest("Permission already assigned or invalid role/permission.");
            return Ok();
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemovePermission([FromBody] AssignPermissionDto dto)
        {
            var removed = await _permissionService.RemovePermissionAsync(dto.RoleId, dto.PermissionId);
            if (!removed) return BadRequest("Permission not assigned.");
            return Ok();
        }

        [HttpGet("role/{roleId}")]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissionsByRole(Guid roleId)
        {
            var permissions = await _permissionService.GetPermissionsByRoleAsync(roleId);
            return Ok(permissions);
        }
    }
}
