using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    [Table("roles")]
    public class Role
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("description")]
        public string? Description { get; set; }
        public ICollection<RolePermission>? RolePermissions { get; set; }
    }
}
