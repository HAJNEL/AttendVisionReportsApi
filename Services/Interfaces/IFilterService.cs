using System.Collections.Generic;
using System.Threading.Tasks;

namespace AttendVisionReportsApi.Services
{
    public interface IFilterService
    {
        Task<IEnumerable<FilterService.EmployeeResult>> GetDepartmentEmployeesAsync(Guid? departmentId);
    }
}
