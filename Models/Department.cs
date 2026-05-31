namespace AttendVisionReportsApi.Models
{
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("departments")]
    public class Department
    {
        [Column("id")] public Guid Id { get; set; }
        [Column("department_name")] public string DepartmentName { get; set; } = "";
        [Column("manager")] public string? Manager { get; set; }
        // removed: payment_rate
        [Column("address_line1")] public string? AddressLine1 { get; set; }
        [Column("address_line2")] public string? AddressLine2 { get; set; }
        [Column("city")] public string? City { get; set; }
        [Column("state")] public string? State { get; set; }
        [Column("postal_code")] public string? PostalCode { get; set; }
        [Column("country")] public string? Country { get; set; }
        [Column("serial_no")] public string? SerialNo { get; set; }
        [Column("companyid")] public Guid? CompanyId { get; set; }
        [Column("company_code")] public string? CompanyCode { get; set; }

        // removed: overtime_payment_rate, overtime_start_after_time, check_in_override_time
    }
}
