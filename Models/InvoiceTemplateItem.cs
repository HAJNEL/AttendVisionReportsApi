namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("invoice_template_items")]
    public class InvoiceTemplateItem
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("template_id")] public Guid TemplateId { get; set; }
        [Column("invoice_item_id")] public Guid? InvoiceItemId { get; set; }
        [Column("sort_order")] public int SortOrder { get; set; }
        [Column("description")] public string Description { get; set; } = "";
        [Column("quantity")] public decimal Quantity { get; set; } = 1;
        [Column("unit_rate")] public decimal UnitRate { get; set; }
        [Column("discount_percent")] public decimal DiscountPercent { get; set; }
    }
}
