namespace AttendVisionReportsApi.DTOs
{
    public record InvoiceSettingsDto(
        Guid Id,
        string IssuerName,
        string? IssuerAddress,
        string? BankName,
        string? AccountHolder,
        string? AccountNumber,
        string? BranchCode,
        string? SwiftCode,
        decimal DefaultVatPercent
    );

    public record UpdateInvoiceSettingsDto(
        string IssuerName,
        string? IssuerAddress,
        string? BankName,
        string? AccountHolder,
        string? AccountNumber,
        string? BranchCode,
        string? SwiftCode,
        decimal DefaultVatPercent
    );
}
