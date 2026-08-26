using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    // Issuer/banking details are a single row, editable from the Invoices
    // admin page's Settings tab and stamped onto every printed invoice.
    public class InvoiceSettingsService(AppDbContext db) : IInvoiceSettingsService
    {
        public async Task<InvoiceSettingsDto> GetAsync(CancellationToken ct = default)
        {
            var settings = await db.InvoiceSettingsEntries.FirstOrDefaultAsync(ct);
            if (settings is null)
            {
                settings = new InvoiceSettings { Id = Guid.NewGuid() };
                db.InvoiceSettingsEntries.Add(settings);
                await db.SaveChangesAsync(ct);
            }
            return Map(settings);
        }

        public async Task<InvoiceSettingsDto> UpdateAsync(UpdateInvoiceSettingsDto dto, CancellationToken ct = default)
        {
            var settings = await db.InvoiceSettingsEntries.FirstOrDefaultAsync(ct);
            if (settings is null)
            {
                settings = new InvoiceSettings { Id = Guid.NewGuid() };
                db.InvoiceSettingsEntries.Add(settings);
            }

            settings.IssuerName = dto.IssuerName;
            settings.IssuerAddress = dto.IssuerAddress;
            settings.BankName = dto.BankName;
            settings.AccountHolder = dto.AccountHolder;
            settings.AccountNumber = dto.AccountNumber;
            settings.BranchCode = dto.BranchCode;
            settings.SwiftCode = dto.SwiftCode;
            settings.DefaultVatPercent = dto.DefaultVatPercent;

            await db.SaveChangesAsync(ct);
            return Map(settings);
        }

        private static InvoiceSettingsDto Map(InvoiceSettings s) =>
            new(s.Id, s.IssuerName, s.IssuerAddress, s.BankName, s.AccountHolder, s.AccountNumber, s.BranchCode, s.SwiftCode, s.DefaultVatPercent);
    }
}
