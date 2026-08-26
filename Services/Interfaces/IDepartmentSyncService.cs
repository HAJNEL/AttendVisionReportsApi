using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IDepartmentSyncService
    {
        Task<DepartmentSyncResult> SyncAllAsync(CancellationToken ct = default);
    }
}
