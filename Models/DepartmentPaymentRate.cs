using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    [Table("department_payment_rates")]
    public class DepartmentPaymentRate
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("department_id")]
        public Guid DepartmentId { get; set; }

        [Column("rate_type")]
        public string RateType { get; set; } = string.Empty; // standard, public_holiday

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("match_key")]
        [MaxLength(20)]
        public string? MatchKey { get; set; }

        [Column("other_label")]
        [MaxLength(40)]
        public string? OtherLabel { get; set; }

        [Column("applies_to")]
        public string AppliesTo { get; set; } = string.Empty; // standard, other
    }
}
