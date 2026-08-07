using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IHikCentralService
    {
        Task<HikCentralPersonListData> SearchPersonsAsync(HikCentralPersonSearchRequest request, CancellationToken ct = default);
        Task<HikCentralPerson?> GetPersonAsync(string personId, CancellationToken ct = default);
        Task<string> AddPersonAsync(HikCentralAddPersonRequest request, CancellationToken ct = default);
        Task UpdatePersonAsync(HikCentralUpdatePersonRequest request, CancellationToken ct = default);
        Task DeletePersonAsync(string personId, CancellationToken ct = default);

        Task<HikCentralOrgListData> GetOrganizationsAsync(int pageNo, int pageSize, CancellationToken ct = default);

        Task<List<HikCentralAcsDevice>> GetAccessControlDevicesAsync(int pageNo, int pageSize, CancellationToken ct = default);
        Task<HikCentralAcsEventListData> SearchAccessEventsAsync(HikCentralAcsEventSearchRequest request, CancellationToken ct = default);
        Task ControlDoorAsync(HikCentralDoorControlRequest request, CancellationToken ct = default);

        Task<HikCentralAccessLevelListData> GetAccessLevelsAsync(int pageNo, int pageSize, CancellationToken ct = default);
        Task AssignAccessLevelAsync(string privilegeGroupId, string personId, CancellationToken ct = default);

        Task<HikCentralFaceListData> SearchFacesAsync(List<string> personIds, CancellationToken ct = default);
        Task<HikCentralPrivilegeByPersonData?> GetPersonPrivilegesAsync(string personId, CancellationToken ct = default);
        Task<List<HikCentralAtsShift>> GetAtsSchedulesAsync(List<string> personIds, string startTime, string endTime, CancellationToken ct = default);
    }
}
