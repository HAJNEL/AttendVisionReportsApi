using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IHikCentralService
    {
        Task<HikCentralPersonListData> SearchPersonsAsync(HikCentralPersonSearchRequest request, CancellationToken ct = default);
        Task<HikCentralPersonListData> SearchPersonsV2Async(HikCentralPersonSearchRequest request, CancellationToken ct = default);
        Task<HikCentralPerson?> GetPersonAsync(string personId, CancellationToken ct = default);
        Task<string> AddPersonAsync(HikCentralAddPersonRequest request, CancellationToken ct = default);
        Task UpdatePersonAsync(HikCentralUpdatePersonRequest request, CancellationToken ct = default);
        Task DeletePersonAsync(string personId, CancellationToken ct = default);

        Task<HikCentralOrgListData> GetOrganizationsAsync(int pageNo, int pageSize, CancellationToken ct = default);

        Task<List<HikCentralAcsDevice>> GetAccessControlDevicesAsync(int pageNo, int pageSize, CancellationToken ct = default);
        Task<HikCentralAcsEventListData> SearchAccessEventsAsync(HikCentralAcsEventSearchRequest request, CancellationToken ct = default);
        Task ControlDoorAsync(HikCentralDoorControlRequest request, CancellationToken ct = default);

        Task<HikCentralAccessLevelListData> GetAccessLevelsAsync(int pageNo, int pageSize, CancellationToken ct = default);
        Task<HikCentralPrivilegeGroupPersonListData> GetAccessLevelPersonsAsync(string privilegeGroupId, int pageNo, int pageSize, CancellationToken ct = default);
        Task AssignAccessLevelAsync(string privilegeGroupId, string personId, CancellationToken ct = default);

        Task<byte[]?> GetPersonPictureAsync(string personId, string picUri, CancellationToken ct = default);
        Task<HikCentralPrivilegeByPersonData?> GetPersonPrivilegesAsync(string personId, CancellationToken ct = default);
        Task<List<HikCentralAtsShift>> GetAtsSchedulesAsync(List<string> personIds, string startTime, string endTime, CancellationToken ct = default);
        Task<System.Text.Json.JsonElement?> GetPersonAttendanceGroupAsync(string personId, CancellationToken ct = default);
        Task<HikCentralAttendanceReportData?> GetAttendanceReportAsync(List<string> personIds, string beginTime, string endTime, int pageNo = 1, int pageSize = 100, CancellationToken ct = default);

        // Debug-only escape hatch for the Test button: every plausible beginTime/
        // endTime format has failed identically against attendance/v1/report, so
        // the actual problem is likely elsewhere in the request shape (sortInfo,
        // personID type, etc.) - this lets the caller try arbitrary raw bodies
        // without a new typed DTO per hypothesis. Returns the raw response text
        // regardless of success/failure so the caller can inspect it directly.
        Task<string> PostAttendanceReportRawAsync(string bodyJson, string? userIdOverride = null, bool includeUserIdHeader = true, CancellationToken ct = default);
    }
}
