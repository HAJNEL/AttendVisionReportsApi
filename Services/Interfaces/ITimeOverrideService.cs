using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface ITimeOverrideService
    {
        Task<IEnumerable<TimeOverrideDto>> GetAllAsync();
        Task<IEnumerable<TimeOverrideDto>> GetByDepartmentIdAsync(Guid departmentId);
        Task<TimeOverrideDto> GetByIdAsync(Guid id);
        Task<TimeOverrideDto> CreateAsync(CreateTimeOverrideDto dto);
        Task<TimeOverrideDto> UpdateAsync(Guid id, UpdateTimeOverrideDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}