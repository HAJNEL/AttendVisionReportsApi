namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("invoice_line_items")]
    public class InvoiceLineItem
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("invoice_id")] public Guid InvoiceId { get; set; }
        [Column("invoice_item_id")] public Guid? InvoiceItemId { get; set; }
        [Column("sort_order")] public int SortOrder { get; set; }
        [Column("description")] public string Description { get; set; } = "";
        [Column("quantity")] public decimal Quantity { get; set; }
        [Column("unit_rate")] public decimal UnitRate { get; set; }
        [Column("discount_percent")] public decimal DiscountPercent { get; set; }
        [Column("line_total")] public decimal LineTotal { get; set; }
    }
}
