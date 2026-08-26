using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/invoice-items"), Authorize]
    public class InvoiceItemsController(IInvoiceItemService invoiceItemService) : ControllerBase
    {
        [HttpGet]
        public Task<List<InvoiceItemDto>> GetAll() => invoiceItemService.GetAllAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceItemDto>> GetById(Guid id)
        {
            var item = await invoiceItemService.GetByIdAsync(id);
            if (item is null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceItemDto>> Create(CreateInvoiceItemDto dto)
        {
            var created = await invoiceItemService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceItemDto>> Update(Guid id, UpdateInvoiceItemDto dto)
        {
            var updated = await invoiceItemService.UpdateAsync(id, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await invoiceItemService.DeleteAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
