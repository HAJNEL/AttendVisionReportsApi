using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    [Table("permissions")]
    public class Permission
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("parentid")]
        public Guid? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public Permission? Parent { get; set; }
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = null!;
        [Column("description")]
        public string? Description { get; set; }
        [Column("uniquecode")]
        public string? UniqueCode { get; set; }
        public ICollection<Permission>? Children { get; set; }
        public ICollection<RolePermission>? RolePermissions { get; set; }
    }
}
