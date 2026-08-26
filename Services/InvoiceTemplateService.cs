using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class InvoiceTemplateService(AppDbContext db) : IInvoiceTemplateService
    {
        public async Task<List<InvoiceTemplateDto>> GetAllAsync(CancellationToken ct = default)
        {
            var templates = await db.InvoiceTemplates
                .Include(t => t.Items)
                .OrderBy(t => t.Name)
                .ToListAsync(ct);
            return templates.Select(Map).ToList();
        }

        public async Task<InvoiceTemplateDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var template = await db.InvoiceTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == id, ct);
            return template is null ? null : Map(template);
        }

        public async Task<InvoiceTemplateDto> CreateAsync(CreateInvoiceTemplateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var template = new InvoiceTemplate
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            };
            ApplyItems(template, dto.Items);

            db.InvoiceTemplates.Add(template);
            await db.SaveChangesAsync(ct);
            return Map(template);
        }

        public async Task<InvoiceTemplateDto?> UpdateAsync(Guid id, UpdateInvoiceTemplateDto dto, CancellationToken ct = default)
        {
            var template = await db.InvoiceTemplates.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == id, ct);
            if (template is null) return null;

            template.Name = dto.Name;
            template.Description = dto.Description;
            template.IsActive = dto.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            db.InvoiceTemplateItems.RemoveRange(template.Items);
            template.Items.Clear();
            ApplyItems(template, dto.Items);

            await db.SaveChangesAsync(ct);
            return Map(template);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var template = await db.InvoiceTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (template is null) return false;
            db.InvoiceTemplates.Remove(template);
            await db.SaveChangesAsync(ct);
            return true;
        }

        private static void ApplyItems(InvoiceTemplate template, List<InvoiceTemplateItemInput> items)
        {
            var order = 0;
            foreach (var item in items)
            {
                template.Items.Add(new InvoiceTemplateItem
                {
                    Id = Guid.NewGuid(),
                    TemplateId = template.Id,
                    InvoiceItemId = item.InvoiceItemId,
                    SortOrder = order++,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitRate = item.UnitRate,
                    DiscountPercent = item.DiscountPercent,
                });
            }
        }

        private static InvoiceTemplateDto Map(InvoiceTemplate t)
        {
            var items = t.Items
                .OrderBy(i => i.SortOrder)
                .Select(i => new InvoiceTemplateItemDto(i.Id, i.InvoiceItemId, i.SortOrder, i.Description, i.Quantity, i.UnitRate, i.DiscountPercent))
                .ToList();
            return new InvoiceTemplateDto(t.Id, t.Name, t.Description, t.IsActive, items);
        }
    }
}
