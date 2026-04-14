using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    [Table("user_roles")]
    public class UserRole
    {
        [Column("user_id")]
        public Guid UserId { get; set; }
        [Column("role_id")]
        public Guid RoleId { get; set; }
        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; }
    }
}
