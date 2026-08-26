using System.Text.Json.Serialization;

namespace AttendVisionReportsApi.DTOs
{
    public record ArtemisResponse<T>(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("msg")] string? Msg,
        [property: JsonPropertyName("data")] T? Data
    );

    public record HikCentralPerson(
        [property: JsonPropertyName("personId")] string? PersonId,
        [property: JsonPropertyName("personCode")] string? PersonCode,
        [property: JsonPropertyName("personName")] string? PersonName,
        [property: JsonPropertyName("personFamilyName")] string? PersonFamilyName,
        [property: JsonPropertyName("personGivenName")] string? PersonGivenName,
        [property: JsonPropertyName("gender")] int? Gender,
        [property: JsonPropertyName("orgIndexCode")] string? OrgIndexCode,
        [property: JsonPropertyName("phoneNo")] string? PhoneNo,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("jobNo")] string? JobNo,
        [property: JsonPropertyName("jobTitle")] string? JobTitle,
        [property: JsonPropertyName("remark")] string? Remark,
        [property: JsonPropertyName("beginTime")] string? BeginTime,
        [property: JsonPropertyName("endTime")] string? EndTime,
        [property: JsonPropertyName("customFieldList")] List<HikCentralCustomField>? CustomFields = null,
        [property: JsonPropertyName("personPhoto")] HikCentralPersonPhoto? PersonPhoto = null
    );

    // Field names match HikCentral's actual personList response (including its
    // "customFiledName" typo) - not the "key"/"value" shape the API docs'
    // condensed schema summary implies.
    public record HikCentralCustomField(
        [property: JsonPropertyName("customFiledName")] string? Key,
        [property: JsonPropertyName("customFieldValue")] string? Value
    );

    // Embedded directly in personList/personInfo responses - no separate
    // lookup call needed to find the picUri, unlike the old (and on this
    // HikCentral edition, unsupported - "product version not supported")
    // /person/face/list approach this replaces.
    public record HikCentralPersonPhoto(
        [property: JsonPropertyName("picUri")] string? PicUri,
        [property: JsonPropertyName("picBigUri")] string? PicBigUri
    );

    // Get person picture - /artemis/api/resource/v1/person/picture_data.
    // Response is raw image bytes (Content-Type: image/jpeg) on success, not
    // a JSON envelope - handled specially in HikCentralService.
    public record HikCentralPictureDataRequest(
        [property: JsonPropertyName("personId")] string PersonId,
        [property: JsonPropertyName("picUri")] string PicUri
    );

    // Door access privileges for a person - /artemis/api/ac/v1/privilege/byPerson
    public record HikCentralPrivilegeByPersonRequest(
        [property: JsonPropertyName("personId")] string PersonId
    );

    public record HikCentralPersonPrivilege(
        [property: JsonPropertyName("templateId")] string? TemplateId,
        [property: JsonPropertyName("templateName")] string? TemplateName
    );

    public record HikCentralPrivilegeByPersonData(
        [property: JsonPropertyName("personPrivilegeList")] List<HikCentralPersonPrivilege>? PersonPrivilegeList
    );

    // Time & Attendance shift schedule - /artemis/api/ats/v1/schedule/list
    public record HikCentralAtsScheduleRequest(
        [property: JsonPropertyName("personIds")] List<string> PersonIds,
        [property: JsonPropertyName("startTime")] string StartTime,
        [property: JsonPropertyName("endTime")] string EndTime
    );

    public record HikCentralAtsShift(
        [property: JsonPropertyName("personId")] string? PersonId,
        [property: JsonPropertyName("date")] string? Date,
        [property: JsonPropertyName("shiftName")] string? ShiftName,
        [property: JsonPropertyName("onDutyTime")] string? OnDutyTime,
        [property: JsonPropertyName("offDutyTime")] string? OffDutyTime
    );

    public record HikCentralAddPersonRequest(
        [property: JsonPropertyName("personCode")] string PersonCode,
        [property: JsonPropertyName("personFamilyName")] string PersonFamilyName,
        [property: JsonPropertyName("personGivenName")] string PersonGivenName,
        [property: JsonPropertyName("orgIndexCode")] string OrgIndexCode,
        [property: JsonPropertyName("gender")] int? Gender,
        [property: JsonPropertyName("phoneNo")] string? PhoneNo,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("remark")] string? Remark,
        [property: JsonPropertyName("beginTime")] string BeginTime,
        [property: JsonPropertyName("endTime")] string EndTime
    );

    public record HikCentralUpdatePersonRequest(
        [property: JsonPropertyName("personId")] string PersonId,
        [property: JsonPropertyName("personFamilyName")] string? PersonFamilyName,
        [property: JsonPropertyName("personGivenName")] string? PersonGivenName,
        [property: JsonPropertyName("orgIndexCode")] string? OrgIndexCode,
        [property: JsonPropertyName("gender")] int? Gender,
        [property: JsonPropertyName("phoneNo")] string? PhoneNo,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("jobNo")] string? JobNo
    );

    public record HikCentralPersonSearchRequest(
        [property: JsonPropertyName("pageNo")] int PageNo,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("orgIndexCode")] string? OrgIndexCode,
        [property: JsonPropertyName("personName")] string? PersonName
    );

    public record HikCentralPersonListData(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("pageNo")] int PageNo,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("list")] List<HikCentralPerson>? List
    );

    public record HikCentralOrganization(
        [property: JsonPropertyName("orgIndexCode")] string? OrgIndexCode,
        [property: JsonPropertyName("orgName")] string? OrgName,
        [property: JsonPropertyName("parentOrgIndexCode")] string? ParentOrgIndexCode
    );

    public record HikCentralOrgListData(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("list")] List<HikCentralOrganization>? List
    );

    public record HikCentralAcsDevice(
        [property: JsonPropertyName("acsDevIndexCode")] string? AcsDevIndexCode,
        [property: JsonPropertyName("acsDevName")] string? AcsDevName,
        [property: JsonPropertyName("acsDevIp")] string? AcsDevIp,
        [property: JsonPropertyName("acsDevCode")] string? AcsDevCode,
        [property: JsonPropertyName("status")] int? Status
    );

    public record HikCentralAcsDeviceListData(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("list")] List<HikCentralAcsDevice>? List
    );

    public record HikCentralAcsEventSearchRequest(
        [property: JsonPropertyName("startTime")] string StartTime,
        [property: JsonPropertyName("endTime")] string EndTime,
        [property: JsonPropertyName("pageNo")] int PageNo,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("personName")] string? PersonName,
        [property: JsonPropertyName("doorIndexCodes")] List<string>? DoorIndexCodes,
        [property: JsonPropertyName("eventType")] int? EventType,
        [property: JsonPropertyName("sortField")] string? SortField = null,
        [property: JsonPropertyName("orderType")] int? OrderType = null
    );

    public record HikCentralAcsEvent(
        [property: JsonPropertyName("eventId")] string? EventId,
        [property: JsonPropertyName("personId")] string? PersonId,
        [property: JsonPropertyName("personName")] string? PersonName,
        [property: JsonPropertyName("doorIndexCode")] string? DoorIndexCode,
        [property: JsonPropertyName("doorName")] string? DoorName,
        [property: JsonPropertyName("eventTime")] string? EventTime,
        [property: JsonPropertyName("eventType")] int? EventType,
        [property: JsonPropertyName("picUri")] string? PicUri
    );

    // HikCentral returns "data": [] (an empty array) instead of an empty
    // envelope object when a search matches nothing, which the default
    // object deserializer rejects. This converter treats that shape as a
    // zero-result page instead of throwing.
    [JsonConverter(typeof(HikCentralAcsEventListDataConverter))]
    public record HikCentralAcsEventListData(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("pageNo")] int PageNo,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("list")] List<HikCentralAcsEvent>? List
    );

    public class HikCentralAcsEventListDataConverter : System.Text.Json.Serialization.JsonConverter<HikCentralAcsEventListData>
    {
        public override HikCentralAcsEventListData? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
                return new HikCentralAcsEventListData(0, 0, 0, []);

            var total = root.TryGetProperty("total", out var t) ? t.GetInt32() : 0;
            var pageNo = root.TryGetProperty("pageNo", out var pn) ? pn.GetInt32() : 0;
            var pageSize = root.TryGetProperty("pageSize", out var ps) ? ps.GetInt32() : 0;
            var list = root.TryGetProperty("list", out var l) && l.ValueKind == System.Text.Json.JsonValueKind.Array
                ? System.Text.Json.JsonSerializer.Deserialize<List<HikCentralAcsEvent>>(l.GetRawText(), options)
                : null;
            return new HikCentralAcsEventListData(total, pageNo, pageSize, list);
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, HikCentralAcsEventListData value, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("total", value.Total);
            writer.WriteNumber("pageNo", value.PageNo);
            writer.WriteNumber("pageSize", value.PageSize);
            writer.WritePropertyName("list");
            System.Text.Json.JsonSerializer.Serialize(writer, value.List, options);
            writer.WriteEndObject();
        }
    }

    public record HikCentralDoorControlRequest(
        [property: JsonPropertyName("doorIndexCode")] string DoorIndexCode,
        [property: JsonPropertyName("controlType")] int ControlType
    );

    public record HikCentralAccessLevelElementBaseInfo(
        [property: JsonPropertyName("Name")] string? Name,
        [property: JsonPropertyName("AreaName")] string? AreaName
    );

    public record HikCentralAccessLevelElementInfo(
        [property: JsonPropertyName("BaseInfo")] HikCentralAccessLevelElementBaseInfo? BaseInfo
    );

    public record HikCentralAccessLevelElement(
        [property: JsonPropertyName("Element")] HikCentralAccessLevelElementInfo? Element
    );

    public record HikCentralAccessLevel(
        [property: JsonPropertyName("privilegeGroupId")] string? PrivilegeGroupId,
        [property: JsonPropertyName("privilegeGroupName")] string? PrivilegeGroupName,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("ElementList")] List<HikCentralAccessLevelElement>? ElementList
    );

    public record HikCentralAccessLevelListData(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("pageNo")] int PageNo,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("list")] List<HikCentralAccessLevel>? List
    );

    public record HikCentralAssignAccessLevelRequest(
        [property: JsonPropertyName("privilegeGroupId")] string PrivilegeGroupId,
        [property: JsonPropertyName("type")] int Type,
        [property: JsonPropertyName("list")] List<HikCentralAssignAccessLevelPerson> List
    );

    public record HikCentralAssignAccessLevelPerson(
        [property: JsonPropertyName("id")] string Id
    );

    // Per the official HikCentral OpenAPI Developer Guide (HIK_API/HikCentral-OpenAPI-Endpoints.md),
    // there's no single-call "get this person's access levels" endpoint - the documented way is to
    // list all groups (HikCentralAccessLevelListData, already above) and check membership per group
    // via /artemis/api/acs/v1/privilege/group/single/personList. The response only carries {id}
    // (personId) per entry despite the docs claiming "See details in PersonInfo".
    public record HikCentralPrivilegeGroupPerson(
        [property: JsonPropertyName("id")] string? Id
    );

    public record HikCentralPrivilegeGroupPersonListData(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("pageNo")] int PageNo,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("list")] List<HikCentralPrivilegeGroupPerson>? List
    );

    // Real documented Time & Attendance endpoint - /artemis/api/attendance/v1/report.
    // Not in this project's original 542-page OpenAPI guide extract (no ATS module
    // there at all - see HikCentral-OpenAPI-Endpoints.md) but confirmed present in
    // both the official tpp.hikvision.com T&A integration page (diagram, response
    // example) and a public HikCentral Professional OpenAPI V3.0.1 Developer Guide
    // PDF (full request/response spec, §5.14.1) - HikCentral added this API in a
    // later OpenAPI version than the guide this project originally sourced. Also
    // requires a "userId" HTTP header (a HikCentral platform user's ID, not the
    // AK/SK integration account) - see HikCentral:UserId in appsettings, unverified
    // against this deployment until tested live.
    public record HikCentralAttendanceReportRequestBody(
        [property: JsonPropertyName("attendanceReportRequest")] HikCentralAttendanceReportRequest AttendanceReportRequest
    );

    public record HikCentralAttendanceReportRequest(
        [property: JsonPropertyName("pageNo")] int PageNo,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("queryInfo")] HikCentralAttendanceReportQueryInfo QueryInfo
    );

    public record HikCentralAttendanceReportQueryInfo(
        [property: JsonPropertyName("personID")] List<string> PersonId,
        [property: JsonPropertyName("beginTime")] string BeginTime,
        [property: JsonPropertyName("endTime")] string EndTime,
        [property: JsonPropertyName("sortInfo")] HikCentralAttendanceReportSortInfo? SortInfo = null
    );

    public record HikCentralAttendanceReportSortInfo(
        [property: JsonPropertyName("sortField")] int SortField,
        [property: JsonPropertyName("sortType")] int SortType
    );

    public record HikCentralAttendanceReportData(
        [property: JsonPropertyName("nextPage")] string? NextPage,
        [property: JsonPropertyName("pageNo")] string? PageNo,
        [property: JsonPropertyName("pageSize")] string? PageSize,
        [property: JsonPropertyName("record")] List<HikCentralAttendanceRecord>? Record
    );

    public record HikCentralAttendanceRecord(
        [property: JsonPropertyName("personInfo")] HikCentralAttendancePersonInfo? PersonInfo,
        [property: JsonPropertyName("date")] string? Date,
        [property: JsonPropertyName("weekDay")] string? WeekDay,
        [property: JsonPropertyName("allDurationTime")] string? AllDurationTime,
        [property: JsonPropertyName("planInfo")] HikCentralAttendancePlanInfo? PlanInfo,
        [property: JsonPropertyName("attendanceBaseInfo")] HikCentralAttendanceBaseInfo? AttendanceBaseInfo
    );

    public record HikCentralAttendancePersonInfo(
        [property: JsonPropertyName("personID")] string? PersonId,
        [property: JsonPropertyName("fullName")] string? FullName,
        [property: JsonPropertyName("personCode")] string? PersonCode,
        [property: JsonPropertyName("orgIndexCode")] string? OrgIndexCode,
        [property: JsonPropertyName("orgName")] string? OrgName
    );

    public record HikCentralAttendancePlanInfo(
        [property: JsonPropertyName("periodID")] string? PeriodId,
        [property: JsonPropertyName("periodName")] string? PeriodName,
        [property: JsonPropertyName("planBeginTime")] string? PlanBeginTime,
        [property: JsonPropertyName("planEndTime")] string? PlanEndTime,
        [property: JsonPropertyName("planWorkDurationTime")] string? PlanWorkDurationTime
    );

    // attendanceStatus: "1"-normal, "2"-late, "3"-early leave, "4"-absent,
    // "5"-late and early leave, "6"-holiday, "7"-unscheduled, "8"-leave.
    public record HikCentralAttendanceBaseInfo(
        [property: JsonPropertyName("beginTime")] string? BeginTime,
        [property: JsonPropertyName("endTime")] string? EndTime,
        [property: JsonPropertyName("attendanceStatus")] string? AttendanceStatus
    );
}
