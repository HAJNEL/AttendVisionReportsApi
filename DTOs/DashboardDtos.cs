namespace AttendVisionReportsApi.DTOs
{
    public record EmployeeKpiDetail(
        string? EmployeeId, 
        string? DepartmentName, 
        string? FullName, 
        string? CheckInTime, 
        string? CheckOutTime, 
        string? LastStatus);

    public record CheckInKpiDetail(
        string? EmployeeId, 
        string? DepartmentName, 
        string? FullName, 
        string? CheckInTime, 
        string? TimeLate, 
        string? TimeEarly);

    public record OnSiteKpiDetail(
        string? EmployeeId, 
        string? DepartmentName, 
        string? FullName, 
        string? TotalTimeWorked, 
        string? TimeSinceLastBreak);

    public record OnBreakKpiDetail(
        string? EmployeeId, 
        string? DepartmentName, 
        string? FullName, 
        string? BreakTimeStartedTimeAgo,
        string? TotalTimeOnBreak);

    public record DashboardKpisResponse
    {
        public IEnumerable<EmployeeKpiDetail> TotalEmployeesDetails { get; init; }
        public IEnumerable<CheckInKpiDetail> CheckinsTodayDetails { get; init; }
        public IEnumerable<OnSiteKpiDetail> OnSiteDetails { get; init; }
        public IEnumerable<OnBreakKpiDetail> OnBreakDetails { get; init; }

        public DashboardKpisResponse(
            IEnumerable<EmployeeKpiDetail> totalEmployeesDetails,
            IEnumerable<CheckInKpiDetail> checkinsTodayDetails,
            IEnumerable<OnSiteKpiDetail> onSiteDetails,
            IEnumerable<OnBreakKpiDetail> onBreakDetails)
        {
            TotalEmployeesDetails = totalEmployeesDetails;
            CheckinsTodayDetails = checkinsTodayDetails;
            OnSiteDetails = onSiteDetails;
            OnBreakDetails = onBreakDetails;
        }

        // Keep the old constructor for backward compatibility if needed
        public DashboardKpisResponse(
            long totalEmployees,
            long checkinsToday,
            long onSiteNow,
            long onBreakNow,
            IEnumerable<EmployeeKpiDetail> totalEmployeesDetails,
            IEnumerable<CheckInKpiDetail> checkinsTodayDetails,
            IEnumerable<OnSiteKpiDetail> onSiteDetails,
            IEnumerable<OnBreakKpiDetail> onBreakDetails)
        {
            TotalEmployeesDetails = totalEmployeesDetails;
            CheckinsTodayDetails = checkinsTodayDetails;
            OnSiteDetails = onSiteDetails;
            OnBreakDetails = onBreakDetails;
        }
    }

    public record LabeledCountResponse(string Label, long Count);

    public record DayAccessRowResponse(string Label, long Count, string Names);

    public record DayEventRowResponse(string Label, string Status, long Count, string Names);

    public class DayPersonRowResponse
    {
        public string person { get; set; } = default!;
        public string? employee_id { get; set; }
        public string department { get; set; } = default!;
        public long event_count { get; set; }
        public string first_time { get; set; } = default!;
        public string last_time { get; set; } = default!;
        public string last_status { get; set; } = default!;
        public double hours_break { get; set; }
        public string break_time { get; set; } = default!;
        public double hours_worked { get; set; }
        public string worked_time { get; set; } = default!;
    }
}
