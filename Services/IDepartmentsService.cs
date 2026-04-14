using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IDepartmentsService
    {
        Task<List<DepartmentResponse>> GetAllAsync();
        Task<DepartmentResponse> CreateAsync(DepartmentInput input);
        Task<DepartmentResponse?> UpdateAsync(int id, DepartmentInput input);
        Task<bool> DeleteAsync(int id);
    }
}
