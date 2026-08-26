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
        [Column("display_name")]
        public string? DisplayName { get; set; }
        [Column("description")]
        public string? Description { get; set; }
        [Column("phone")]
        public string? Phone { get; set; }
        [Column("email")]
        public string? Email { get; set; }
        [Column("address")]
        public string? Address { get; set; }
    }
}
