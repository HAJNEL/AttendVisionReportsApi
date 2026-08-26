namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    // Decision log for the employee approval workflow - ApproveAsync/RejectAsync
    // currently delete the temp_employees row either way, leaving no trace.
    // This captures who decided what, when, and why, independent of whether
    // the underlying employee/department still exists afterward.
    [Table("temp_employee_history")]
    public class TempEmployeeHistory
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("request_type")] public string RequestType { get; set; } = "";
        [Column("employee_id")] public Guid? EmployeeId { get; set; }
        [Column("employee_no")] public string? EmployeeNo { get; set; }
        [Column("first_name")] public string? FirstName { get; set; }
        [Column("last_name")] public string? LastName { get; set; }
        [Column("department_id")] public Guid? DepartmentId { get; set; }
        [Column("submitted_by")] public Guid? SubmittedBy { get; set; }
        [Column("decided_by")] public Guid? DecidedBy { get; set; }
        [Column("decision")] public string Decision { get; set; } = ""; // Approved | Rejected
        [Column("reason")] public string? Reason { get; set; }
        [Column("submitted_at")] public DateTime SubmittedAt { get; set; }
        [Column("decided_at")] public DateTime DecidedAt { get; set; }
    }
}
