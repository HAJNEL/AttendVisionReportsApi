using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    [Table("department_users")]
    public class DepartmentUser
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("department_id")]
        public Guid DepartmentId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; }

        public Department? Department { get; set; }
        public User? User { get; set; }
    }
}