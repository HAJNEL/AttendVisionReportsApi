namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("device_licenses")]
    public class DeviceLicense
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("hik_dev_index_code")] public string HikDevIndexCode { get; set; } = "";
        [Column("company_id")] public Guid? CompanyId { get; set; }
        [Column("status")] public string Status { get; set; } = "Active";
        [Column("issue_date")] public DateOnly IssueDate { get; set; }
        [Column("expiry_date")] public DateOnly ExpiryDate { get; set; }
        [Column("notes")] public string? Notes { get; set; }
    }
}
