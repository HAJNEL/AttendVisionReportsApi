using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IDepartmentsService
    {
        Task<List<DepartmentResponse>> GetAllAsync();
        Task<List<DepartmentResponse>> GetAllForUserAsync(System.Security.Claims.ClaimsPrincipal user);
        Task<DepartmentResponse> CreateAsync(DepartmentInput input);
        Task<DepartmentResponse?> UpdateAsync(Guid id, DepartmentInput input);
        Task<bool> DeleteAsync(Guid id);
    }
}
