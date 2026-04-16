using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentUsersController : ControllerBase
    {
        private readonly IDepartmentUserService _departmentUserService;
        public DepartmentUsersController(IDepartmentUserService departmentUserService)
        {
            _departmentUserService = departmentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _departmentUserService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _departmentUserService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentUserDto dto)
        {
            var result = await _departmentUserService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentUserDto dto)
        {
            var result = await _departmentUserService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _departmentUserService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("by-department/{departmentId}")]
        public async Task<IActionResult> GetUsersForDepartment(Guid departmentId)
        {
            var result = await _departmentUserService.GetUsersForDepartmentAsync(departmentId);
            return Ok(result);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetDepartmentsForUser(Guid userId)
        {
            var result = await _departmentUserService.GetDepartmentsForUserAsync(userId);
            return Ok(result);
        }
    }
}