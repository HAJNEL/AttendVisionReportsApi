using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services.Interfaces
{
    public interface ITimeManagementService
    {
        Task<IEnumerable<DaySummaryRow>> GetDaySummaryAsync(string date, Guid? departmentId, string? employeeId, Guid userId);
        Task<IEnumerable<AccessRecordDto>> GetUserRecordsAsync(string date, string employeeId, Guid userId);
        Task<IEnumerable<TimeManagementIssue>> GetUserIssuesAsync(string date, string employeeId, Guid userId);
        Task<AccessRecordDto> CreateAccessRecordAsync(CreateAccessRecordDto dto);
        Task<AccessRecordDto> UpdateAccessRecordAsync(long id, string time, string attendanceStatus);
        Task DeleteAccessRecordAsync(long id);
        Task<AutoFixPreviewResponse> GetAutoFixPreviewAsync(string date, string employeeId, Guid userId);
        Task ApplyAutoFixAsync(AutoFixApplyRequest request);
        Task<TimeManagementConfigDto> GetConfigAsync(Guid userId);
        Task<TimeManagementConfigDto> SaveConfigAsync(TimeManagementConfigDto dto, Guid userId);
    }
}
