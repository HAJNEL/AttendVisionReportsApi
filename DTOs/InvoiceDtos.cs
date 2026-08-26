namespace AttendVisionReportsApi.DTOs
{
    public record InvoiceLineItemDto(
        Guid Id,
        Guid? InvoiceItemId,
        int SortOrder,
        string Description,
        decimal Quantity,
        decimal UnitRate,
        decimal DiscountPercent,
        decimal LineTotal
    );

    public record InvoiceLineItemInput(
        Guid? InvoiceItemId,
        string Description,
        decimal Quantity,
        decimal UnitRate,
        decimal DiscountPercent
    );

    public record InvoiceSummaryDto(
        Guid Id,
        string InvoiceNumber,
        Guid? CompanyId,
        string? CompanyName,
        string BillToName,
        DateOnly InvoiceDate,
        bool IsCod,
        DateOnly? DueDate,
        string Status,
        decimal Total,
        decimal AmountDue
    );

    public record InvoiceDto(
        Guid Id,
        string InvoiceNumber,
        Guid? CompanyId,
        string? CompanyName,
        string BillToName,
        string? BillToPhone,
        string? BillToEmail,
        string? BillToAddress,
        DateOnly InvoiceDate,
        bool IsCod,
        DateOnly? DueDate,
        string Status,
        string? Notes,
        decimal VatPercent,
        decimal Subtotal,
        decimal VatAmount,
        decimal Total,
        decimal AmountPaid,
        decimal AmountDue,
        string AmountInWords,
        List<InvoiceLineItemDto> LineItems,
        DateTime CreatedAt
    );

    public record CreateInvoiceDto(
        Guid? CompanyId,
        string BillToName,
        string? BillToPhone,
        string? BillToEmail,
        string? BillToAddress,
        DateOnly InvoiceDate,
        bool IsCod,
        DateOnly? DueDate,
        string? Notes,
        decimal VatPercent,
        List<InvoiceLineItemInput> LineItems
    );

    public record UpdateInvoiceDto(
        Guid? CompanyId,
        string BillToName,
        string? BillToPhone,
        string? BillToEmail,
        string? BillToAddress,
        DateOnly InvoiceDate,
        bool IsCod,
        DateOnly? DueDate,
        string Status,
        string? Notes,
        decimal VatPercent,
        List<InvoiceLineItemInput> LineItems
    );

    public record UpdateInvoiceStatusDto(string Status);
}
