namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("invoice_templates")]
    public class InvoiceTemplate
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("name")] public string Name { get; set; } = "";
        [Column("description")] public string? Description { get; set; }
        [Column("is_active")] public bool IsActive { get; set; } = true;
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }

        public List<InvoiceTemplateItem> Items { get; set; } = new();
    }
}
