using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllAsync();
        Task<RoleDto?> GetByIdAsync(Guid id);
        Task<RoleDto> CreateAsync(CreateRoleDto dto);
        Task<RoleDto?> UpdateAsync(Guid id, UpdateRoleDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> AssignRoleAsync(Guid userId, Guid roleId);
        Task<bool> RemoveRoleAsync(Guid userId, Guid roleId);

        // Permission management for roles
        Task<bool> AssignPermissionAsync(Guid roleId, Guid permissionId);
        Task<bool> RemovePermissionAsync(Guid roleId, Guid permissionId);
        Task<IEnumerable<PermissionDto>> GetPermissionsAsync(Guid roleId);
    }
}
