namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("invoices")]
    public class Invoice
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("invoice_number")] public string InvoiceNumber { get; set; } = "";
        [Column("company_id")] public Guid? CompanyId { get; set; }
        [Column("bill_to_name")] public string BillToName { get; set; } = "";
        [Column("bill_to_phone")] public string? BillToPhone { get; set; }
        [Column("bill_to_email")] public string? BillToEmail { get; set; }
        [Column("bill_to_address")] public string? BillToAddress { get; set; }
        [Column("invoice_date")] public DateOnly InvoiceDate { get; set; }
        [Column("is_cod")] public bool IsCod { get; set; }
        [Column("due_date")] public DateOnly? DueDate { get; set; }
        [Column("status")] public string Status { get; set; } = "Draft";
        [Column("notes")] public string? Notes { get; set; }
        [Column("vat_percent")] public decimal VatPercent { get; set; }
        [Column("subtotal")] public decimal Subtotal { get; set; }
        [Column("vat_amount")] public decimal VatAmount { get; set; }
        [Column("total")] public decimal Total { get; set; }
        [Column("amount_paid")] public decimal AmountPaid { get; set; }
        [Column("created_by")] public Guid? CreatedBy { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }

        public List<InvoiceLineItem> LineItems { get; set; } = new();
    }
}
