using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/filter")]
[Authorize]
public class FilterController(IFilterService filterService) : ControllerBase
{
    [HttpGet("employees")]
    public async Task<IActionResult> GetDepartmentEmployees([FromQuery] Guid? departmentId, [FromQuery] Guid? attendanceGroupId) =>
        Ok(await filterService.GetDepartmentEmployeesAsync(departmentId, attendanceGroupId, User));
}
