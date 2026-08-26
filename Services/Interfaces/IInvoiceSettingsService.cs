using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IInvoiceSettingsService
    {
        Task<InvoiceSettingsDto> GetAsync(CancellationToken ct = default);
        Task<InvoiceSettingsDto> UpdateAsync(UpdateInvoiceSettingsDto dto, CancellationToken ct = default);
    }
}
