using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeLeaveController : ControllerBase
    {
        private readonly IEmployeeLeaveService _service;
        public EmployeeLeaveController(IEmployeeLeaveService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeLeaveDto>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("ranges")]
        public async Task<ActionResult<IEnumerable<EmployeeLeaveRangeDto>>> GetRanges([FromQuery] Guid? departmentId, [FromQuery] string? employeeId, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate)
        {
            var result = await _service.GetRangesAsync(departmentId, employeeId, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeLeaveDto>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<EmployeeLeaveDto>>> Create(CreateEmployeeLeaveDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return Ok(created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeLeaveDto>> Update(Guid id, [FromBody] UpdateEmployeeLeaveDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpDelete("range")]
        public async Task<ActionResult<int>> DeleteRange([FromQuery] string employeeId, [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
        {
            var count = await _service.DeleteRangeAsync(employeeId, startDate, endDate);
            return Ok(count);
        }
    }
}