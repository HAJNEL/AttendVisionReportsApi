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
        [property: JsonPropertyName("remark")] string? Remark,
        [property: JsonPropertyName("beginTime")] string? BeginTime,
        [property: JsonPropertyName("endTime")] string? EndTime,
        [property: JsonPropertyName("customFields")] List<HikCentralCustomField>? CustomFields = null
    );

    public record HikCentralCustomField(
        [property: JsonPropertyName("key")] string? Key,
        [property: JsonPropertyName("value")] string? Value
    );

    // Person face/photo lookup - /artemis/api/resource/v1/person/face/list
    public record HikCentralFaceListRequest(
        [property: JsonPropertyName("personIds")] List<string> PersonIds
    );

    public record HikCentralFace(
        [property: JsonPropertyName("personId")] string? PersonId,
        [property: JsonPropertyName("faceId")] string? FaceId,
        [property: JsonPropertyName("faceData")] string? FaceData
    );

    public record HikCentralFaceListData(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("list")] List<HikCentralFace>? List
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
        [property: JsonPropertyName("indexCode")] string? IndexCode,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("devIndexCode")] string? DevIndexCode,
        [property: JsonPropertyName("regionIndexCode")] string? RegionIndexCode
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
        [property: JsonPropertyName("eventType")] int? EventType
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

    public record HikCentralAcsEventListData(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("pageNo")] int PageNo,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("list")] List<HikCentralAcsEvent>? List
    );

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
}
