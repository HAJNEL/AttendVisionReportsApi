namespace AttendVisionReportsApi.DTOs
{
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? UniqueCode { get; set; }
    }

    public class CreatePermissionDto
    {
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? UniqueCode { get; set; }
    }

    public class UpdatePermissionDto
    {
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? UniqueCode { get; set; }
    }

    public class AssignPermissionDto
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
    }
}
