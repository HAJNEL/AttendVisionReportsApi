using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/departments")]
    public class DepartmentsController(IDepartmentsService departmentsService, IDepartmentSyncService departmentSyncService) : ControllerBase
    {

        [HttpGet]
        public Task<List<DepartmentResponse>> GetAllForUser() => departmentsService.GetAllForUserAsync(User);

        [HttpGet("all")]
        public Task<List<DepartmentResponse>> GetAll() => departmentsService.GetAllAsync();

        [HttpPost]
        public async Task<ActionResult<DepartmentResponse>> Create(DepartmentInput input)
        {
            try
            {
                var dept = await departmentsService.CreateAsync(input);
                return CreatedAtAction(nameof(GetAll), dept);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<DepartmentResponse>> Update(Guid id, DepartmentInput input)
        {
            try
            {
                var dept = await departmentsService.UpdateAsync(id, input);
                if (dept is null) return NotFound();
                return Ok(dept);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await departmentsService.DeleteAsync(id)) return NotFound();
            return NoContent();
        }

        [HttpPost("sync")]
        public async Task<ActionResult<DepartmentSyncResult>> Sync(CancellationToken ct) =>
            Ok(await departmentSyncService.SyncAllAsync(ct));
    }
}
