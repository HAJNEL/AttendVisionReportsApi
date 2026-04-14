namespace AttendVisionReportsApi.DTOs
{
    public class CompanyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CreateCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
