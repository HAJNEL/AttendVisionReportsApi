using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    public class EmployeeLeave
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("department_id")]
        public Guid DepartmentId { get; set; }
        [Column("type")]
        public required string Type { get; set; }
        [Column("from_date")]
        public DateOnly FromDate { get; set; }
        [Column("to_date")]
        public DateOnly ToDate { get; set; }
        [Column("from_time")]
        public TimeOnly? FromTime { get; set; }
        [Column("to_time")]
        public TimeOnly? ToTime { get; set; }
        [Column("employee_id")]
        public required string EmployeeId { get; set; }
        [Column("full_name")]
        public required string FullName { get; set; }
    }
}