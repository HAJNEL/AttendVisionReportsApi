namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("companies")]
    public class Company
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("description")]
        public string? Description { get; set; }
    }
}
