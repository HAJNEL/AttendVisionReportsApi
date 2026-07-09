namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("report_config")]
    public class ReportConfig
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("company_id")]
        public Guid CompanyId { get; set; }

        [Column("month_start_day")]
        public int? MonthStartDay { get; set; }

        [Column("month_end_day")]
        public int? MonthEndDay { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
