using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IInvoiceService
    {
        Task<List<InvoiceSummaryDto>> GetAllAsync(CancellationToken ct = default);
        Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto, Guid? createdBy, CancellationToken ct = default);
        Task<InvoiceDto?> UpdateAsync(Guid id, UpdateInvoiceDto dto, CancellationToken ct = default);
        Task<InvoiceDto?> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
