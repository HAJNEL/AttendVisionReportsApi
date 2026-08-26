namespace AttendVisionReportsApi.DTOs
{
    public class AttendanceGroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateAttendanceGroupDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateAttendanceGroupDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
