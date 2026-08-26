namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("temp_employees")]
    public class TempEmployee
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("employee_id")] public Guid? EmployeeId { get; set; }
        [Column("has_changes")] public bool HasChanges { get; set; }
        // 'Create' | 'Update' | 'Delete'. Replaces the old inferred-from-
        // EmployeeId-null logic, which couldn't distinguish an edit from a
        // delete once both carry a non-null EmployeeId.
        [Column("request_type")] public string RequestType { get; set; } = "Update";
        [Column("employee_no")] public string? EmployeeNo { get; set; }
        [Column("first_name")] public string? FirstName { get; set; }
        [Column("last_name")] public string? LastName { get; set; }
        [Column("department_id")] public Guid? DepartmentId { get; set; }
        [Column("gender")] public int? Gender { get; set; }
        [Column("phone_no")] public string? PhoneNo { get; set; }
        [Column("email")] public string? Email { get; set; }
        [Column("job_no")] public string? JobNo { get; set; }
        [Column("remark")] public string? Remark { get; set; }
        [Column("photo_base64")] public string? PhotoBase64 { get; set; }
        [Column("submitted_by")] public Guid? SubmittedBy { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }

        public Employee? Employee { get; set; }
        public Department? Department { get; set; }
    }
}
