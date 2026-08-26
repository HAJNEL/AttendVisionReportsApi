namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("invoice_settings")]
    public class InvoiceSettings
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("issuer_name")] public string IssuerName { get; set; } = "AttendVision";
        [Column("issuer_address")] public string? IssuerAddress { get; set; }
        [Column("bank_name")] public string? BankName { get; set; }
        [Column("account_holder")] public string? AccountHolder { get; set; }
        [Column("account_number")] public string? AccountNumber { get; set; }
        [Column("branch_code")] public string? BranchCode { get; set; }
        [Column("swift_code")] public string? SwiftCode { get; set; }
        [Column("default_vat_percent")] public decimal DefaultVatPercent { get; set; }
    }
}
