namespace AttendVisionReportsApi.DTOs
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string UniqueCode { get; set; } = string.Empty;
    }

    public class CreateRoleDto
    {
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string UniqueCode { get; set; } = string.Empty;
    }

    public class UpdateRoleDto
    {
        public Guid? ParentId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? UniqueCode { get; set; }
    }

    public class AssignRoleDto
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}
