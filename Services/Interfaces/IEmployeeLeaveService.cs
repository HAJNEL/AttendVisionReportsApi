using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IEmployeeLeaveService
    {
        Task<IEnumerable<EmployeeLeaveDto>> GetAllAsync();
        Task<IEnumerable<EmployeeLeaveRangeDto>> GetRangesAsync(Guid? departmentId, string employeeId, DateOnly? startDate, DateOnly? endDate);
        Task<EmployeeLeaveDto> GetByIdAsync(Guid id);
        Task<IEnumerable<EmployeeLeaveDto>> CreateAsync(CreateEmployeeLeaveDto dto);
        Task<EmployeeLeaveDto> UpdateAsync(Guid id, UpdateEmployeeLeaveDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<int> DeleteRangeAsync(string employeeId, DateOnly startDate, DateOnly endDate);
    }
}