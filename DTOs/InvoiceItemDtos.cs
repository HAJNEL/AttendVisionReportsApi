namespace AttendVisionReportsApi.DTOs
{
    public record InvoiceItemDto(
        Guid Id,
        string Name,
        string? Description,
        decimal UnitRate,
        string? UnitLabel,
        string BillingType,
        bool IsActive
    );

    public record CreateInvoiceItemDto(
        string Name,
        string? Description,
        decimal UnitRate,
        string? UnitLabel,
        string BillingType,
        bool IsActive
    );

    public record UpdateInvoiceItemDto(
        string Name,
        string? Description,
        decimal UnitRate,
        string? UnitLabel,
        string BillingType,
        bool IsActive
    );
}
