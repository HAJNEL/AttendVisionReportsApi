namespace AttendVisionReportsApi.DTOs
{
    public record DeviceLicenseInput(
        Guid? CompanyId,
        string Status,
        DateOnly IssueDate,
        DateOnly ExpiryDate,
        string? Notes
    );

    public record DeviceRenewInput(int ExtensionMonths);

    public record DeviceEarliestLogResponse(DateOnly? EarliestDate);

    public record DeviceResponse(
        string HikDevIndexCode,
        string? Name,
        string? Ip,
        string? SerialCode,
        int? OnlineStatus,
        Guid? LicenseId,
        Guid? CompanyId,
        string? CompanyName,
        string LicenseStatus,
        DateOnly? IssueDate,
        DateOnly? ExpiryDate,
        string? Notes
    );
}
