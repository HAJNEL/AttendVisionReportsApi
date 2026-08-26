using System.Security.Claims;
using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface ITempEmployeeService
    {
        Task<List<TempEmployeeListItem>> GetPendingAsync(ClaimsPrincipal user, CancellationToken ct = default);
        Task<TempEmployeeListItem> SubmitAsync(TempEmployeeSubmitRequest request, Guid submittedByUserId, CancellationToken ct = default);
        Task<TempEmployeeListItem> SubmitDeleteRequestAsync(Guid employeeId, string? reason, Guid submittedByUserId, CancellationToken ct = default);
        Task<TempEmployeeListItem> UpdateAsync(Guid tempId, TempEmployeeSubmitRequest request, Guid callerUserId, bool isAdmin, CancellationToken ct = default);
        Task ApproveAsync(Guid tempId, Guid decidedByUserId, bool isAdmin, CancellationToken ct = default);
        Task RejectAsync(Guid tempId, Guid decidedByUserId, bool isAdmin, string? reason, CancellationToken ct = default);
    }
}
