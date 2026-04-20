using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendVisionReportsApi.Models
{
    public class TimeOverride
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("department_id")]

        public Guid DepartmentId { get; set; }
        [Column("from_time")]
        public TimeSpan FromTime { get; set; }
        [Column("to_time")]
        public TimeSpan ToTime { get; set; }
        [Column("override_time")]
        public TimeSpan OverrideTime { get; set; }

        public Department Department { get; set; }
    }
}