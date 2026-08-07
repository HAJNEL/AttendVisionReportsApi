namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("employee_access_levels")]
    public class EmployeeAccessLevel
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("employee_id")] public Guid EmployeeId { get; set; }
        [Column("template_id")] public string? TemplateId { get; set; }
        [Column("template_name")] public string? TemplateName { get; set; }
    }
}
