using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    [Table("role_permissions")]
    public class RolePermission
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("role_id")]
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
        [Column("permission_id")]
        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
