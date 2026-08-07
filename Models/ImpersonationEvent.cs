namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("impersonation_events")]
    public class ImpersonationEvent
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("admin_user_id")] public Guid AdminUserId { get; set; }
        [Column("target_user_id")] public Guid TargetUserId { get; set; }
        [Column("started_at")] public DateTime StartedAt { get; set; }
        [Column("ended_at")] public DateTime? EndedAt { get; set; }
    }
}
