namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("employees")]
    public class Employee
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("hikcentral_person_id")] public string HikCentralPersonId { get; set; } = "";
        [Column("employee_no")] public string? EmployeeNo { get; set; }
        [Column("full_name")] public string? FullName { get; set; }
        [Column("first_name")] public string? FirstName { get; set; }
        [Column("last_name")] public string? LastName { get; set; }
        [Column("gender")] public int? Gender { get; set; }
        [Column("department_id")] public Guid? DepartmentId { get; set; }
        [Column("org_index_code")] public string? OrgIndexCode { get; set; }
        [Column("position")] public string? Position { get; set; }
        [Column("phone_no")] public string? PhoneNo { get; set; }
        [Column("email")] public string? Email { get; set; }
        [Column("job_no")] public string? JobNo { get; set; }
        [Column("remark")] public string? Remark { get; set; }
        [Column("begin_time")] public DateTime? BeginTime { get; set; }
        [Column("end_time")] public DateTime? EndTime { get; set; }
        [Column("photo_base64")] public string? PhotoBase64 { get; set; }
        [Column("current_shift_name")] public string? CurrentShiftName { get; set; }
        [Column("current_shift_on_duty")] public string? CurrentShiftOnDuty { get; set; }
        [Column("current_shift_off_duty")] public string? CurrentShiftOffDuty { get; set; }
        [Column("last_synced_at")] public DateTime? LastSyncedAt { get; set; }

        public Department? Department { get; set; }
    }
}
