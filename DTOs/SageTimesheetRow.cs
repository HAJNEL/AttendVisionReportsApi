namespace AttendVisionReportsApi.DTOs
{
    public class SageTimesheetRow
    {
        public string Empno { get; set; }
        public string Emp_Fullname { get; set; }
        public double Normal_Hours { get; set; }
        public double Overtime_Hours { get; set; }
        public double Public_Holiday_Hours { get; set; }
    }
}