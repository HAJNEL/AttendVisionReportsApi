using System;

namespace AttendVisionReportsApi.DTOs
{
    public class CreateDepartmentUserDto
    {
        public Guid DepartmentId { get; set; }
        public Guid UserId { get; set; }
    }
}