using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IPermissionService
    {
        Task<IEnumerable<PermissionDto>> GetAllAsync();
        Task<PermissionDto?> GetByIdAsync(Guid id);
        Task<PermissionDto> CreateAsync(CreatePermissionDto dto);
        Task<PermissionDto?> UpdateAsync(Guid id, UpdatePermissionDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> AssignPermissionsAsync(Guid roleId, List<Guid> permissionIds);
        Task<bool> RemovePermissionAsync(Guid roleId, Guid permissionId);
        Task<IEnumerable<PermissionDto>> GetPermissionsByRoleAsync(Guid roleId);
    }
}
