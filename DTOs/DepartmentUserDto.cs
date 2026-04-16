using System;

namespace AttendVisionReportsApi.DTOs
{
    public class DepartmentUserDto
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid UserId { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}