using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    [Table("roles")]
    public class Role
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("parentid")]
        public Guid? ParentId { get; set; }
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("description")]
        public string? Description { get; set; }
        [Column("uniquecode")]
        public string UniqueCode { get; set; } = string.Empty;
    }
}
