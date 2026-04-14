using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/departments")]
    public class DepartmentsController(IDepartmentsService departmentsService) : ControllerBase
    {
        [HttpGet]
        public Task<List<DepartmentResponse>> GetAll() => departmentsService.GetAllAsync();

        [HttpPost]
        public async Task<ActionResult<DepartmentResponse>> Create(DepartmentInput input)
        {
            var dept = await departmentsService.CreateAsync(input);
            return CreatedAtAction(nameof(GetAll), dept);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<DepartmentResponse>> Update(int id, DepartmentInput input)
        {
            var dept = await departmentsService.UpdateAsync(id, input);
            if (dept is null) return NotFound();
            return Ok(dept);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await departmentsService.DeleteAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
