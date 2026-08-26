namespace AttendVisionReportsApi.DTOs
{
    public record TempEmployeeListItem(
        Guid Id,
        Guid? EmployeeId,
        string Type,
        bool HasChanges,
        string? EmployeeNo,
        string? FirstName,
        string? LastName,
        int? Gender,
        Guid? DepartmentId,
        string? DepartmentName,
        string? PhoneNo,
        string? Email,
        string? JobNo,
        string? Remark,
        string? PhotoBase64,
        Guid? SubmittedBy,
        string? SubmittedByName,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public record TempEmployeeSubmitRequest(
        Guid? EmployeeId,
        string EmployeeNo,
        string FirstName,
        string LastName,
        Guid DepartmentId,
        int? Gender,
        string? PhoneNo,
        string? Email,
        string? JobNo,
        string? Remark,
        string? PhotoBase64
    );

    public record TempEmployeeDeleteRequest(string? Reason);
}
