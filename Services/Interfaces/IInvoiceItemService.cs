using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IInvoiceItemService
    {
        Task<List<InvoiceItemDto>> GetAllAsync(CancellationToken ct = default);
        Task<InvoiceItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<InvoiceItemDto> CreateAsync(CreateInvoiceItemDto dto, CancellationToken ct = default);
        Task<InvoiceItemDto?> UpdateAsync(Guid id, UpdateInvoiceItemDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
