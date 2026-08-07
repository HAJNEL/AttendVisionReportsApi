using AttendVisionReportsApi.DTOs;
using System.Net.Http.Json;

namespace AttendVisionReportsApi.Services
{
    public class HikCentralService(HttpClient http) : IHikCentralService
    {
        public async Task<HikCentralPersonListData> SearchPersonsAsync(HikCentralPersonSearchRequest request, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/person/personList", request, ct);
            return await ReadAsync<HikCentralPersonListData>(response, ct)
                   ?? new HikCentralPersonListData(0, request.PageNo, request.PageSize, []);
        }

        public async Task<HikCentralPerson?> GetPersonAsync(string personId, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/person/personId/personInfo", new { personId }, ct);
            return await ReadAsync<HikCentralPerson>(response, ct);
        }

        public async Task<string> AddPersonAsync(HikCentralAddPersonRequest request, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/person/single/add", request, ct);
            var personId = await ReadAsync<string>(response, ct);
            return personId ?? throw new InvalidOperationException("HikCentral did not return a personId.");
        }

        public async Task UpdatePersonAsync(HikCentralUpdatePersonRequest request, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/person/single/update", request, ct);
            await EnsureSuccessAsync(response, ct);
        }

        public async Task DeletePersonAsync(string personId, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/person/batch/delete", new { personIds = new[] { personId } }, ct);
            await EnsureSuccessAsync(response, ct);
        }

        public async Task<HikCentralOrgListData> GetOrganizationsAsync(int pageNo, int pageSize, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/org/orgList", new { pageNo, pageSize }, ct);
            return await ReadAsync<HikCentralOrgListData>(response, ct) ?? new HikCentralOrgListData(0, []);
        }

        public async Task<List<HikCentralAcsDevice>> GetAccessControlDevicesAsync(int pageNo, int pageSize, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/acsDevice/acsDeviceList", new { pageNo, pageSize }, ct);
            var data = await ReadAsync<HikCentralAcsDeviceListData>(response, ct);
            return data?.List ?? [];
        }

        public async Task<HikCentralAcsEventListData> SearchAccessEventsAsync(HikCentralAcsEventSearchRequest request, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/acs/v1/door/events", request, ct);
            return await ReadAsync<HikCentralAcsEventListData>(response, ct)
                   ?? new HikCentralAcsEventListData(0, request.PageNo, request.PageSize, []);
        }

        public async Task ControlDoorAsync(HikCentralDoorControlRequest request, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/acs/v1/door/doControl", request, ct);
            await EnsureSuccessAsync(response, ct);
        }

        public async Task<HikCentralAccessLevelListData> GetAccessLevelsAsync(int pageNo, int pageSize, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/acs/v1/privilege/group", new { pageNo, pageSize, type = 1 }, ct);
            return await ReadAsync<HikCentralAccessLevelListData>(response, ct) ?? new HikCentralAccessLevelListData(0, pageNo, pageSize, []);
        }

        public async Task AssignAccessLevelAsync(string privilegeGroupId, string personId, CancellationToken ct = default)
        {
            var request = new HikCentralAssignAccessLevelRequest(privilegeGroupId, 1, [new HikCentralAssignAccessLevelPerson(personId)]);
            var response = await http.PostAsJsonAsync("/artemis/api/acs/v1/privilege/group/single/addPersons", request, ct);
            await EnsureSuccessAsync(response, ct);
        }

        public async Task<HikCentralFaceListData> SearchFacesAsync(List<string> personIds, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/person/face/list", new HikCentralFaceListRequest(personIds), ct);
            return await ReadAsync<HikCentralFaceListData>(response, ct) ?? new HikCentralFaceListData(0, []);
        }

        public async Task<HikCentralPrivilegeByPersonData?> GetPersonPrivilegesAsync(string personId, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/ac/v1/privilege/byPerson", new HikCentralPrivilegeByPersonRequest(personId), ct);
            return await ReadAsync<HikCentralPrivilegeByPersonData>(response, ct);
        }

        public async Task<List<HikCentralAtsShift>> GetAtsSchedulesAsync(List<string> personIds, string startTime, string endTime, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/ats/v1/schedule/list", new HikCentralAtsScheduleRequest(personIds, startTime, endTime), ct);
            return await ReadAsync<List<HikCentralAtsShift>>(response, ct) ?? [];
        }

        private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
        {
            response.EnsureSuccessStatusCode();
            var envelope = await response.Content.ReadFromJsonAsync<ArtemisResponse<T>>(cancellationToken: ct);
            if (envelope is null) return default;
            if (envelope.Code != "0")
                throw new InvalidOperationException($"HikCentral API error {envelope.Code}: {envelope.Msg}");
            return envelope.Data;
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
        {
            response.EnsureSuccessStatusCode();
            var envelope = await response.Content.ReadFromJsonAsync<ArtemisResponse<object>>(cancellationToken: ct);
            if (envelope is not null && envelope.Code != "0")
                throw new InvalidOperationException($"HikCentral API error {envelope.Code}: {envelope.Msg}");
        }
    }
}
