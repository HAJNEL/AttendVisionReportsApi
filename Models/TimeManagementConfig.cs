namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("time_management_config")]
    public class TimeManagementConfig
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("company_id")]
        public Guid CompanyId { get; set; }

        // ── Rule toggles ────────────────────────────────────────────────────
        [Column("detect_missing_check_in")]
        public bool DetectMissingCheckIn { get; set; } = true;

        [Column("detect_missing_check_out")]
        public bool DetectMissingCheckOut { get; set; } = true;

        [Column("detect_missing_break")]
        public bool DetectMissingBreak { get; set; } = true;

        [Column("detect_duplicates")]
        public bool DetectDuplicates { get; set; } = true;

        [Column("enable_double_shift")]
        public bool EnableDoubleShift { get; set; } = true;

        // ── Rule parameters ─────────────────────────────────────────────────
        [Column("workday_hours")]
        public decimal WorkdayHours { get; set; } = 8m;

        [Column("double_shift_hours")]
        public decimal DoubleShiftHours { get; set; } = 16m;

        [Column("break_default_minutes")]
        public int BreakDefaultMinutes { get; set; } = 30;

        [Column("check_in_offset_minutes")]
        public int CheckInOffsetMinutes { get; set; } = 8;

        [Column("check_out_offset_minutes")]
        public int CheckOutOffsetMinutes { get; set; } = 8;

        // remove_failed_then_second | always_second
        [Column("duplicate_keep_strategy")]
        [MaxLength(40)]
        public string DuplicateKeepStrategy { get; set; } = "remove_failed_then_second";

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
