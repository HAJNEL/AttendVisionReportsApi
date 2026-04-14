using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    public class RolePermission
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
