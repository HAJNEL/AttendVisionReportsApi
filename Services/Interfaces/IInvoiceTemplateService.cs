using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IInvoiceTemplateService
    {
        Task<List<InvoiceTemplateDto>> GetAllAsync(CancellationToken ct = default);
        Task<InvoiceTemplateDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<InvoiceTemplateDto> CreateAsync(CreateInvoiceTemplateDto dto, CancellationToken ct = default);
        Task<InvoiceTemplateDto?> UpdateAsync(Guid id, UpdateInvoiceTemplateDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
