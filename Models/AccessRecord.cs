namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("access_records")]
    public class AccessRecord
    {
        [Column("id")] public long Id { get; set; }
        [Column("employee_id")] public string? EmployeeId { get; set; }
        [Column("person_name")] public string? PersonName { get; set; }
        [Column("department")] public string? Department { get; set; }
        [Column("access_date")] public DateOnly AccessDate { get; set; }
        [Column("access_time")] public TimeOnly AccessTime { get; set; }
        [Column("access_datetime")] public DateTime AccessDatetime { get; set; }
        [Column("attendance_status")] public string? AttendanceStatus { get; set; }
        [Column("authentication_result")] public string? AuthenticationResult { get; set; }
        [Column("failed")] public bool? Failed { get; set; }
        [Column("check_in")] public bool? CheckIn { get; set; }
    }
}
