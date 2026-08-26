using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AttendVisionReportsApi.Services
{
    public interface IFilterService
    {
        Task<IEnumerable<FilterService.EmployeeResult>> GetDepartmentEmployeesAsync(Guid? departmentId, Guid? attendanceGroupId, ClaimsPrincipal? user = null);
    }
}
