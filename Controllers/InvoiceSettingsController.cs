using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/invoice-settings"), Authorize]
    public class InvoiceSettingsController(IInvoiceSettingsService invoiceSettingsService) : ControllerBase
    {
        [HttpGet]
        public Task<InvoiceSettingsDto> Get() => invoiceSettingsService.GetAsync();

        [HttpPut]
        public Task<InvoiceSettingsDto> Update(UpdateInvoiceSettingsDto dto) => invoiceSettingsService.UpdateAsync(dto);
    }
}
