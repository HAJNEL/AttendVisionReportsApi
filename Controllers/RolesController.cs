using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll() => Ok(await _roleService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetById(Guid id)
        {
            var role = await _roleService.GetByIdAsync(id);
            if (role == null) return NotFound();
            return Ok(role);
        }

        [HttpPost]
        public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto dto)
        {
            var role = await _roleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<RoleDto>> Update(Guid id, [FromBody] UpdateRoleDto dto)
        {
            var role = await _roleService.UpdateAsync(id, dto);
            if (role == null) return NotFound();
            return Ok(role);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _roleService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            var assigned = await _roleService.AssignRoleAsync(dto.UserId, dto.RoleId);
            if (!assigned) return BadRequest("Role already assigned or invalid user/role.");
            return Ok();
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleDto dto)
        {
            var removed = await _roleService.RemoveRoleAsync(dto.UserId, dto.RoleId);
            if (!removed) return BadRequest("Role not assigned.");
            return Ok();
        }

        [HttpPost("assign-permission")]
        public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionDto dto)
        {
            var assigned = await _roleService.AssignPermissionAsync(dto.RoleId, dto.PermissionId);
            if (!assigned) return BadRequest("Permission already assigned or invalid role/permission.");
            return Ok();
        }

        [HttpPost("remove-permission")]
        public async Task<IActionResult> RemovePermission([FromBody] AssignPermissionDto dto)
        {
            var removed = await _roleService.RemovePermissionAsync(dto.RoleId, dto.PermissionId);
            if (!removed) return BadRequest("Permission not assigned.");
            return Ok();
        }

        [HttpGet("{id}/permissions")]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions(Guid id)
        {
            var permissions = await _roleService.GetPermissionsAsync(id);
            return Ok(permissions);
        }
    }
}
