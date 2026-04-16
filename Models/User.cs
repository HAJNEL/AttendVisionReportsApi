namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("users")]
    public class User
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("username")] public string Username { get; set; } = "";
        [Column("email")] public string Email { get; set; } = "";
        [Column("password_hash")] public string PasswordHash { get; set; } = "";
        [Column("first_name")] public string? FirstName { get; set; }
        [Column("last_name")] public string? LastName { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
