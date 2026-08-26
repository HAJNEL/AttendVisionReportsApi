using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class InvoiceItemService(AppDbContext db) : IInvoiceItemService
    {
        public async Task<List<InvoiceItemDto>> GetAllAsync(CancellationToken ct = default)
        {
            return await db.InvoiceItems
                .OrderBy(i => i.Name)
                .Select(i => new InvoiceItemDto(i.Id, i.Name, i.Description, i.UnitRate, i.UnitLabel, i.BillingType, i.IsActive))
                .ToListAsync(ct);
        }

        public async Task<InvoiceItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var item = await db.InvoiceItems.FirstOrDefaultAsync(i => i.Id == id, ct);
            return item is null ? null : Map(item);
        }

        public async Task<InvoiceItemDto> CreateAsync(CreateInvoiceItemDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var item = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                UnitRate = dto.UnitRate,
                UnitLabel = dto.UnitLabel,
                BillingType = dto.BillingType,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.InvoiceItems.Add(item);
            await db.SaveChangesAsync(ct);
            return Map(item);
        }

        public async Task<InvoiceItemDto?> UpdateAsync(Guid id, UpdateInvoiceItemDto dto, CancellationToken ct = default)
        {
            var item = await db.InvoiceItems.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (item is null) return null;

            item.Name = dto.Name;
            item.Description = dto.Description;
            item.UnitRate = dto.UnitRate;
            item.UnitLabel = dto.UnitLabel;
            item.BillingType = dto.BillingType;
            item.IsActive = dto.IsActive;
            item.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Map(item);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var item = await db.InvoiceItems.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (item is null) return false;
            db.InvoiceItems.Remove(item);
            await db.SaveChangesAsync(ct);
            return true;
        }

        private static InvoiceItemDto Map(InvoiceItem i) =>
            new(i.Id, i.Name, i.Description, i.UnitRate, i.UnitLabel, i.BillingType, i.IsActive);
    }
}
