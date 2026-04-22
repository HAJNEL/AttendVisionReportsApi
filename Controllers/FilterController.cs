using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/filter")]
public class FilterController(IFilterService filterService) : ControllerBase
{
    [HttpGet("employees")]
    public async Task<IActionResult> GetDepartmentEmployees([FromQuery] Guid? departmentId) =>
        Ok(await filterService.GetDepartmentEmployeesAsync(departmentId));
}
