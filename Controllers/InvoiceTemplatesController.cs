using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/invoice-templates"), Authorize]
    public class InvoiceTemplatesController(IInvoiceTemplateService invoiceTemplateService) : ControllerBase
    {
        [HttpGet]
        public Task<List<InvoiceTemplateDto>> GetAll() => invoiceTemplateService.GetAllAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceTemplateDto>> GetById(Guid id)
        {
            var template = await invoiceTemplateService.GetByIdAsync(id);
            if (template is null) return NotFound();
            return Ok(template);
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceTemplateDto>> Create(CreateInvoiceTemplateDto dto)
        {
            var created = await invoiceTemplateService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceTemplateDto>> Update(Guid id, UpdateInvoiceTemplateDto dto)
        {
            var updated = await invoiceTemplateService.UpdateAsync(id, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await invoiceTemplateService.DeleteAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
