namespace AttendVisionReportsApi.DTOs
{
    public record InvoiceTemplateItemDto(
        Guid Id,
        Guid? InvoiceItemId,
        int SortOrder,
        string Description,
        decimal Quantity,
        decimal UnitRate,
        decimal DiscountPercent
    );

    public record InvoiceTemplateItemInput(
        Guid? InvoiceItemId,
        string Description,
        decimal Quantity,
        decimal UnitRate,
        decimal DiscountPercent
    );

    public record InvoiceTemplateDto(
        Guid Id,
        string Name,
        string? Description,
        bool IsActive,
        List<InvoiceTemplateItemDto> Items
    );

    public record CreateInvoiceTemplateDto(
        string Name,
        string? Description,
        bool IsActive,
        List<InvoiceTemplateItemInput> Items
    );

    public record UpdateInvoiceTemplateDto(
        string Name,
        string? Description,
        bool IsActive,
        List<InvoiceTemplateItemInput> Items
    );
}
