using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AttendVisionReportsApi.Services
{
    public interface IDepartmentUserService
    {
        Task<IEnumerable<DepartmentUserDto>> GetAllAsync();
        Task<DepartmentUserDto?> GetByIdAsync(Guid id);
        Task<DepartmentUserDto> CreateAsync(CreateDepartmentUserDto dto);
        Task<DepartmentUserDto?> UpdateAsync(Guid id, UpdateDepartmentUserDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<DepartmentUserDto>> GetUsersForDepartmentAsync(Guid departmentId);
        Task<IEnumerable<DepartmentUserDto>> GetDepartmentsForUserAsync(Guid userId);
    }
}