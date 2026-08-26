using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IEmployeeSyncService
    {
        Task<EmployeeSyncResult> SyncAllAsync(CancellationToken ct = default);
        Task<EmployeeListItem> CreateAsync(EmployeeCreateRequest request, CancellationToken ct = default);
        Task<EmployeeListItem> UpdateAsync(Guid id, EmployeeUpdateRequest request, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task<EmployeeHikCentralTestResult> TestFetchAsync(Guid id, CancellationToken ct = default);
    }
}
