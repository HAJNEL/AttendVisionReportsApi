using System;
using System.Collections.Generic;

namespace AttendVisionReportsApi.DTOs
{
    public class EmployeeLeaveDto
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public string Type { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public TimeOnly? FromTime { get; set; }
        public TimeOnly? ToTime { get; set; }
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
    }

    public class CreateEmployeeLeaveDto
    {
        public Guid DepartmentId { get; set; }
        public string Type { get; set; }
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public TimeOnly? FromTime { get; set; }
        public TimeOnly? ToTime { get; set; }
    }

    public class UpdateEmployeeLeaveDto
    {
        public Guid DepartmentId { get; set; }
        public string Type { get; set; }
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public TimeOnly? FromTime { get; set; }
        public TimeOnly? ToTime { get; set; }
    }

    public class EmployeeLeaveRangeDto
    {
        public Guid Id { get; set; }
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Type { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public TimeOnly? FromTime { get; set; }
        public TimeOnly? ToTime { get; set; }
    }
}