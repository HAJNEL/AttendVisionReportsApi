using AttendVisionReportsApi.DTOs;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AttendVisionReportsApi.Services
{
    public class HikCentralService(HttpClient http, IConfiguration configuration) : IHikCentralService
    {
        private static readonly JsonSerializerOptions RelaxedJsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public async Task<HikCentralPersonListData> SearchPersonsAsync(HikCentralPersonSearchRequest request, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/person/personList", request, ct);
            return await ReadAsync<HikCentralPersonListData>(response, ct)
                   ?? new HikCentralPersonListData(0, request.PageNo, request.PageSize, []);
        }

        // v2 is documented to return jobTitle (HikCentral's UI "Position" field)
        // where v1 doesn't - kept as a fallback attempt, but on this deployment
        // it (and the ATS/T&A endpoints below) reject with "HikCentral API error
        // 8: This product version is not supported", so it currently just
        // surfaces that error rather than data.
        public async Task<HikCentralPersonListData> SearchPersonsV2Async(HikCentralPersonSearchRequest request, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v2/person/personList", request, ct);
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

        // Per HIK_API/HikCentral-OpenAPI-Endpoints.md and openapi-api.yaml
        // ("Delete a person", /api/resource/v1/person/single/delete),
        // adminPassword is a required field on this call - not optional,
        // and not the same as this app's own API credentials. Configure it
        // under HikCentral:AdminPassword before enabling employee deletion.
        public async Task DeletePersonAsync(string personId, CancellationToken ct = default)
        {
            var adminPassword = configuration["HikCentral:AdminPassword"];
            if (string.IsNullOrEmpty(adminPassword))
                throw new InvalidOperationException(
                    "Deleting a HikCentral person requires HikCentral:AdminPassword to be configured - add it to appsettings before approving delete requests.");

            var response = await http.PostAsJsonAsync(
                "/artemis/api/resource/v1/person/single/delete",
                new { personId, adminPassword },
                ct);
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

        // Documented endpoint for finding a person's access levels: no reverse
        // by-person lookup exists, so this checks membership of one group at a
        // time - see GetPersonPrivilegesAsync below for the old (undocumented,
        // wrong-prefix) attempt this replaces for the Test button.
        public async Task<HikCentralPrivilegeGroupPersonListData> GetAccessLevelPersonsAsync(string privilegeGroupId, int pageNo, int pageSize, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/acs/v1/privilege/group/single/personList", new { pageNo, pageSize, type = 1, privilegeGroupId }, ct);
            return await ReadAsync<HikCentralPrivilegeGroupPersonListData>(response, ct) ?? new HikCentralPrivilegeGroupPersonListData(0, pageNo, pageSize, []);
        }

        public async Task AssignAccessLevelAsync(string privilegeGroupId, string personId, CancellationToken ct = default)
        {
            var request = new HikCentralAssignAccessLevelRequest(privilegeGroupId, 1, [new HikCentralAssignAccessLevelPerson(personId)]);
            var response = await http.PostAsJsonAsync("/artemis/api/acs/v1/privilege/group/single/addPersons", request, ct);
            await EnsureSuccessAsync(response, ct);
        }

        public async Task<byte[]?> GetPersonPictureAsync(string personId, string picUri, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/resource/v1/person/picture_data", new HikCentralPictureDataRequest(personId, picUri), ct);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is not null && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return await response.Content.ReadAsByteArrayAsync(ct);

            // Errors come back as the usual JSON envelope instead of an image.
            var envelope = await response.Content.ReadFromJsonAsync<ArtemisResponse<object>>(cancellationToken: ct);
            throw new InvalidOperationException($"HikCentral API error {envelope?.Code}: {envelope?.Msg}");
        }

        // Confirmed against the official OpenAPI Developer Guide: this path
        // (ac/v1, not acs/v1) doesn't exist in the documented API surface at
        // all - kept only because EmployeeSyncService's regular sync still
        // calls it as a best-effort attempt. GetAccessLevelPersonsAsync above
        // is the documented replacement, used by the Test button.
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

        // Not in this project's local OpenAPI spec at all - path/field names are
        // unverified against this deployment (same trap that bit
        // customFieldList/jobTitle). Returned as a raw JsonElement rather than a
        // typed record so the Test button shows exactly what comes back instead
        // of silently dropping fields that don't match a guessed schema. This is
        // also an /ats/v1/ path like schedule/list and privilege/byPerson, both
        // of which this deployment already rejects with "product version not
        // supported" - expect the same until confirmed otherwise.
        public async Task<JsonElement?> GetPersonAttendanceGroupAsync(string personId, CancellationToken ct = default)
        {
            var response = await http.PostAsJsonAsync("/artemis/api/ats/v1/attendance/person/group", new { personId }, ct);
            return await ReadAsync<JsonElement>(response, ct);
        }

        // Real documented replacement for GetAtsSchedulesAsync/GetPersonAttendanceGroupAsync
        // above (both confirmed dead - wrong module, "error 8" - on this deployment) -
        // see the long comment on HikCentralAttendanceReportRequestBody in HikCentralDtos.cs.
        // Requires a "userId" header identifying a HikCentral platform user, which
        // ArtemisSigningHandler's AK/SK signature doesn't cover (only x-ca-* headers are
        // canonicalized), so it's safe to add without touching the signing logic.
        public async Task<HikCentralAttendanceReportData?> GetAttendanceReportAsync(List<string> personIds, string beginTime, string endTime, int pageNo = 1, int pageSize = 100, CancellationToken ct = default)
        {
            var body = new HikCentralAttendanceReportRequestBody(
                new HikCentralAttendanceReportRequest(pageNo, pageSize,
                    new HikCentralAttendanceReportQueryInfo(personIds, beginTime, endTime)));

            // System.Text.Json's default encoder escapes the "+" in beginTime/
            // endTime's offset (e.g. "...T00:00:00+02:00") as a unicode escape
            // sequence, which is valid JSON. Kept in case some other HikCentral
            // endpoint hits the same escaping quirk, but confirmed NOT sufficient
            // on its own to fix this endpoint's "beginTime parameter error" -
            // the literal "+" byte-for-byte matching the guide's format still
            // gets rejected, so something else about the request is wrong too.
            using var request = new HttpRequestMessage(HttpMethod.Post, "/artemis/api/attendance/v1/report")
            {
                Content = JsonContent.Create(body, options: RelaxedJsonOptions)
            };
            request.Headers.TryAddWithoutValidation("userId", configuration["HikCentral:UserId"] ?? "");

            var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var raw = await response.Content.ReadAsStringAsync(ct);
            var envelope = JsonSerializer.Deserialize<ArtemisResponse<HikCentralAttendanceReportData>>(raw);
            if (envelope is null) return null;
            // Includes the full raw response body in the error, not just msg -
            // this endpoint's error has resisted two straight fix attempts
            // (offset value, then "+" escaping) so surfacing everything HikCentral
            // actually sent back (in case there's a field beyond code/msg/data
            // our DTO silently drops) beats guessing at a third fix blind.
            if (envelope.Code != "0")
                throw new InvalidOperationException($"HikCentral API error {envelope.Code}: {envelope.Msg} | raw: {raw}");
            return envelope.Data;
        }

        public async Task<string> PostAttendanceReportRawAsync(string bodyJson, string? userIdOverride = null, bool includeUserIdHeader = true, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/artemis/api/attendance/v1/report")
            {
                Content = new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json")
            };
            if (includeUserIdHeader)
                request.Headers.TryAddWithoutValidation("userId", userIdOverride ?? configuration["HikCentral:UserId"] ?? "");
            var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
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
