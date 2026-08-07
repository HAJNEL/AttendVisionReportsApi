namespace AttendVisionReportsApi.DTOs
{
    public record EmployeeSyncResult(int TotalPersons, int Created, int Updated, List<string> Warnings);

    public record EmployeeListItem(
        Guid Id,
        string HikCentralPersonId,
        string? EmployeeNo,
        string? FullName,
        Guid? DepartmentId,
        string? DepartmentName,
        string? Position,
        string? OrgIndexCode,
        string? PhoneNo,
        string? Email,
        string? PhotoBase64,
        string? CurrentShiftName,
        string? CurrentShiftOnDuty,
        string? CurrentShiftOffDuty,
        List<string> AccessLevelNames,
        DateTime? BeginTime,
        DateTime? EndTime,
        DateTime? LastSyncedAt
    );
}
