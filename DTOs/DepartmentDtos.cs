namespace AttendVisionReportsApi.DTOs
{

    public record DepartmentInput(
        string DepartmentName,
        string? Manager,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? State,
        string? PostalCode,
        string? Country,
        string? SerialNo,
        Guid? CompanyId,
        string? CompanyCode
    );


    public record DepartmentResponse(
        Guid Id,
        string DepartmentName,
        string? Manager,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? State,
        string? PostalCode,
        string? Country,
        string? SerialNo,
        Guid? CompanyId,
        string? CompanyCode,
        string? CompanyName
    );
}
