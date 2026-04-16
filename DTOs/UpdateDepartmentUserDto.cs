using System;

namespace AttendVisionReportsApi.DTOs
{
    public class UpdateDepartmentUserDto
    {
        public Guid DepartmentId { get; set; }
        public Guid UserId { get; set; }
    }
}