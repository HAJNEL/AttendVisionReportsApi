using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/invoices"), Authorize]
    public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
    {
        [HttpGet]
        public Task<List<InvoiceSummaryDto>> GetAll() => invoiceService.GetAllAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
        {
            var invoice = await invoiceService.GetByIdAsync(id);
            if (invoice is null) return NotFound();
            return Ok(invoice);
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceDto dto)
        {
            Helpers.ClaimsHelper.TryGetUserId(User, out var userId);
            var created = await invoiceService.CreateAsync(dto, userId == Guid.Empty ? null : userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceDto>> Update(Guid id, UpdateInvoiceDto dto)
        {
            var updated = await invoiceService.UpdateAsync(id, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<InvoiceDto>> UpdateStatus(Guid id, UpdateInvoiceStatusDto dto)
        {
            var updated = await invoiceService.UpdateStatusAsync(id, dto.Status);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await invoiceService.DeleteAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
