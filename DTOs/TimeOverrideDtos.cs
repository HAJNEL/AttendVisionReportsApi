using System;

namespace AttendVisionReportsApi.DTOs
{
    public class TimeOverrideDto
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public TimeSpan OverrideTime { get; set; }
    }

    public class CreateTimeOverrideDto
    {
        public Guid DepartmentId { get; set; }
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public TimeSpan OverrideTime { get; set; }
    }

    public class UpdateTimeOverrideDto
    {
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
        public TimeSpan? OverrideTime { get; set; }
    }
}