using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceGroupsController : ControllerBase
    {
        private readonly IAttendanceGroupService _attendanceGroupService;
        public AttendanceGroupsController(IAttendanceGroupService attendanceGroupService)
        {
            _attendanceGroupService = attendanceGroupService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceGroupDto>>> GetAll() =>
            Ok(await _attendanceGroupService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<AttendanceGroupDto>> GetById(Guid id)
        {
            var group = await _attendanceGroupService.GetByIdAsync(id);
            if (group == null) return NotFound();
            return Ok(group);
        }

        [HttpPost]
        public async Task<ActionResult<AttendanceGroupDto>> Create([FromBody] CreateAttendanceGroupDto dto)
        {
            var group = await _attendanceGroupService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AttendanceGroupDto>> Update(Guid id, [FromBody] UpdateAttendanceGroupDto dto)
        {
            var group = await _attendanceGroupService.UpdateAsync(id, dto);
            if (group == null) return NotFound();
            return Ok(group);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _attendanceGroupService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/employees")]
        public async Task<IActionResult> SetMembers(Guid id, [FromBody] AssignAttendanceGroupMembersRequest request)
        {
            var ok = await _attendanceGroupService.SetMembersAsync(id, request.EmployeeIds);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
