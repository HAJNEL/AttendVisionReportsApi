using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Helpers;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class InvoiceService(AppDbContext db) : IInvoiceService
    {
        public async Task<List<InvoiceSummaryDto>> GetAllAsync(CancellationToken ct = default)
        {
            var companies = await db.Companies.ToDictionaryAsync(c => c.Id, c => c.Name, ct);
            var invoices = await db.Invoices
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.CreatedAt)
                .ToListAsync(ct);

            return invoices.Select(i => MapSummary(i, companies)).ToList();
        }

        public async Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var invoice = await db.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id, ct);
            if (invoice is null) return null;
            return Map(invoice, await GetCompanyNameAsync(invoice.CompanyId, ct));
        }

        public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto, Guid? createdBy, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = await GenerateInvoiceNumberAsync(dto.InvoiceDate, ct),
                CompanyId = dto.CompanyId,
                BillToName = dto.BillToName,
                BillToPhone = dto.BillToPhone,
                BillToEmail = dto.BillToEmail,
                BillToAddress = dto.BillToAddress,
                InvoiceDate = dto.InvoiceDate,
                IsCod = dto.IsCod,
                DueDate = dto.IsCod ? null : dto.DueDate,
                Status = "Draft",
                Notes = dto.Notes,
                VatPercent = dto.VatPercent,
                AmountPaid = 0,
                CreatedBy = createdBy,
                CreatedAt = now,
                UpdatedAt = now,
            };

            ApplyLineItems(invoice, dto.LineItems);
            RecalculateTotals(invoice);

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(ct);

            return Map(invoice, await GetCompanyNameAsync(invoice.CompanyId, ct));
        }

        public async Task<InvoiceDto?> UpdateAsync(Guid id, UpdateInvoiceDto dto, CancellationToken ct = default)
        {
            var invoice = await db.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id, ct);
            if (invoice is null) return null;

            invoice.CompanyId = dto.CompanyId;
            invoice.BillToName = dto.BillToName;
            invoice.BillToPhone = dto.BillToPhone;
            invoice.BillToEmail = dto.BillToEmail;
            invoice.BillToAddress = dto.BillToAddress;
            invoice.InvoiceDate = dto.InvoiceDate;
            invoice.IsCod = dto.IsCod;
            invoice.DueDate = dto.IsCod ? null : dto.DueDate;
            invoice.Status = dto.Status;
            invoice.Notes = dto.Notes;
            invoice.VatPercent = dto.VatPercent;
            invoice.UpdatedAt = DateTime.UtcNow;

            db.InvoiceLineItems.RemoveRange(invoice.LineItems);
            invoice.LineItems.Clear();
            ApplyLineItems(invoice, dto.LineItems);
            RecalculateTotals(invoice);

            await db.SaveChangesAsync(ct);
            return Map(invoice, await GetCompanyNameAsync(invoice.CompanyId, ct));
        }

        public async Task<InvoiceDto?> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
        {
            var invoice = await db.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id, ct);
            if (invoice is null) return null;

            invoice.Status = status;
            invoice.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Map(invoice, await GetCompanyNameAsync(invoice.CompanyId, ct));
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (invoice is null) return false;
            db.Invoices.Remove(invoice);
            await db.SaveChangesAsync(ct);
            return true;
        }

        private Task<string?> GetCompanyNameAsync(Guid? companyId, CancellationToken ct) =>
            companyId is Guid cid
                ? db.Companies.Where(c => c.Id == cid).Select(c => c.Name).FirstOrDefaultAsync(ct)
                : Task.FromResult<string?>(null);

        private static void ApplyLineItems(Invoice invoice, List<InvoiceLineItemInput> items)
        {
            var order = 0;
            foreach (var item in items)
            {
                var lineTotal = Math.Round(item.Quantity * item.UnitRate * (1 - item.DiscountPercent / 100m), 2);
                invoice.LineItems.Add(new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    InvoiceItemId = item.InvoiceItemId,
                    SortOrder = order++,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitRate = item.UnitRate,
                    DiscountPercent = item.DiscountPercent,
                    LineTotal = lineTotal,
                });
            }
        }

        private static void RecalculateTotals(Invoice invoice)
        {
            invoice.Subtotal = invoice.LineItems.Sum(l => l.LineTotal);
            invoice.VatAmount = Math.Round(invoice.Subtotal * invoice.VatPercent / 100m, 2);
            invoice.Total = invoice.Subtotal + invoice.VatAmount;
        }

        // Mirrors the format used on the sample invoices this feature was
        // modeled on: INV-{yyyyMMdd}-{4 random digits}, not sequential.
        private async Task<string> GenerateInvoiceNumberAsync(DateOnly invoiceDate, CancellationToken ct)
        {
            var datePart = invoiceDate.ToString("yyyyMMdd");
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var candidate = $"INV-{datePart}-{Random.Shared.Next(1000, 10000)}";
                if (!await db.Invoices.AnyAsync(i => i.InvoiceNumber == candidate, ct))
                {
                    return candidate;
                }
            }
            return $"INV-{datePart}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        }

        private static InvoiceSummaryDto MapSummary(Invoice i, Dictionary<Guid, string> companies)
        {
            var companyName = i.CompanyId is Guid cid && companies.TryGetValue(cid, out var n) ? n : null;
            return new InvoiceSummaryDto(
                i.Id, i.InvoiceNumber, i.CompanyId, companyName, i.BillToName,
                i.InvoiceDate, i.IsCod, i.DueDate, i.Status, i.Total, i.Total - i.AmountPaid
            );
        }

        private static InvoiceDto Map(Invoice i, string? companyName)
        {
            var lineItems = i.LineItems
                .OrderBy(l => l.SortOrder)
                .Select(l => new InvoiceLineItemDto(l.Id, l.InvoiceItemId, l.SortOrder, l.Description, l.Quantity, l.UnitRate, l.DiscountPercent, l.LineTotal))
                .ToList();

            return new InvoiceDto(
                i.Id, i.InvoiceNumber, i.CompanyId, companyName,
                i.BillToName, i.BillToPhone, i.BillToEmail, i.BillToAddress,
                i.InvoiceDate, i.IsCod, i.DueDate, i.Status, i.Notes,
                i.VatPercent, i.Subtotal, i.VatAmount, i.Total, i.AmountPaid, i.Total - i.AmountPaid,
                NumberToWordsHelper.ToWords(i.Total),
                lineItems, i.CreatedAt
            );
        }
    }
}
