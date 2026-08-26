namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("invoice_items")]
    public class InvoiceItem
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("name")] public string Name { get; set; } = "";
        [Column("description")] public string? Description { get; set; }
        [Column("unit_rate")] public decimal UnitRate { get; set; }
        [Column("unit_label")] public string? UnitLabel { get; set; }
        [Column("billing_type")] public string BillingType { get; set; } = "OneTime";
        [Column("is_active")] public bool IsActive { get; set; } = true;
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
