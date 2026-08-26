namespace AttendVisionReportsApi.DTOs
{
    using System.Text.Json;

    public record EmployeeSyncResult(int TotalPersons, int Created, int Updated, List<string> Warnings);

    // Debug/test endpoint - dumps as much information as possible about one
    // employee: everything already stored locally, plus every live HikCentral
    // call the sync pipeline makes (or could make) for them, with per-call
    // errors, so field mismatches (like the jobTitle/customField saga) can be
    // inspected directly instead of guessed at. AttendanceGroup is a raw
    // JsonElement (not a typed record) since its endpoint/field names are
    // unverified against this deployment - showing whatever actually comes
    // back matters more here than a clean shape.
    public record EmployeeHikCentralTestResult(
        string? EmployeeName,
        EmployeeListItem LocalRecord,
        HikCentralPerson? V1Person,
        string? V1Error,
        HikCentralPerson? V2Person,
        string? V2Error,
        List<HikCentralPersonPrivilege> AccessLevels,
        string? AccessLevelsError,
        List<string> AccessLevelsViaGroups,
        string? AccessLevelsViaGroupsError,
        string? LivePhotoBase64,
        string? LivePhotoError,
        HikCentralAtsShift? Shift,
        string? ShiftError,
        JsonElement? AttendanceGroup,
        string? AttendanceGroupError,
        List<HikCentralAttendanceRecord> AttendanceReport,
        string? AttendanceReportError,
        string? AttendanceReportRequestJson,
        Dictionary<string, string> AttendanceReportFormatTrials
    );

    public record EmployeeListItem(
        Guid Id,
        string HikCentralPersonId,
        string? EmployeeNo,
        string? FullName,
        string? FirstName,
        string? LastName,
        int? Gender,
        Guid? DepartmentId,
        string? DepartmentName,
        string? Position,
        string? OrgIndexCode,
        string? PhoneNo,
        string? Email,
        string? JobNo,
        string? PhotoBase64,
        string? CurrentShiftName,
        string? CurrentShiftOnDuty,
        string? CurrentShiftOffDuty,
        List<string> AccessLevelNames,
        DateTime? BeginTime,
        DateTime? EndTime,
        DateTime? LastSyncedAt,
        Guid? AttendanceGroupId = null,
        string? AttendanceGroupName = null
    );

    public record AssignAttendanceGroupMembersRequest(List<Guid> EmployeeIds);

    public record EmployeeCreateRequest(
        string EmployeeNo,
        string FirstName,
        string LastName,
        Guid DepartmentId,
        int? Gender,
        string? PhoneNo,
        string? Email,
        string? Remark,
        string? PhotoBase64 = null
    );

    public record EmployeeUpdateRequest(
        string FirstName,
        string LastName,
        Guid? DepartmentId,
        int? Gender,
        string? PhoneNo,
        string? Email,
        string? JobNo,
        string? PhotoBase64 = null
    );
}
