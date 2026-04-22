using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<PermissionDto>> GetPermissionsForUserAsync(Guid userId);
        Task<IEnumerable<PermissionDto>?> GetPermissionsForCurrentUserAsync(System.Security.Claims.ClaimsPrincipal user);

        Task<IEnumerable<DepartmentResponse>> GetDepartmentsForUserAsync(Guid userId);
        Task<IEnumerable<UserDto>> GetUsersForDepartmentAsync(Guid departmentId);
    }
}
