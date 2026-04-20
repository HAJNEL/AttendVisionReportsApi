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
    public class TimeOverridesController : ControllerBase
    {
        private readonly ITimeOverrideService _service;
        public TimeOverridesController(ITimeOverrideService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeOverrideDto>>> GetAll([FromQuery] Guid? departmentId)
        {
            IEnumerable<TimeOverrideDto> result;
            if (departmentId.HasValue)
            {
                result = await _service.GetByDepartmentIdAsync(departmentId.Value);
            }
            else
            {
                result = await _service.GetAllAsync();
            }
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TimeOverrideDto>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<TimeOverrideDto>> Create(CreateTimeOverrideDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TimeOverrideDto>> Update(Guid id, UpdateTimeOverrideDto dto)
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
    }
}