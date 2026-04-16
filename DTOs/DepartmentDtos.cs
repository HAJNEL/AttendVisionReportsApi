namespace AttendVisionReportsApi.DTOs
{
    public record DepartmentInput(
        string DepartmentName, string? Manager, decimal? PaymentRate,
        string? AddressLine1, string? AddressLine2,
        string? City, string? State, string? PostalCode, string? Country,
        string? SerialNo,
        Guid? CompanyId);

    public record DepartmentResponse(
        Guid Id, string DepartmentName, string? Manager, double? PaymentRate,
        string? AddressLine1, string? AddressLine2,
        string? City, string? State, string? PostalCode, string? Country,
        string? SerialNo,
        Guid? CompanyId,
        string? CompanyName);
}
