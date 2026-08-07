using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IEmployeeSyncService
    {
        Task<EmployeeSyncResult> SyncAllAsync(CancellationToken ct = default);
    }
}
