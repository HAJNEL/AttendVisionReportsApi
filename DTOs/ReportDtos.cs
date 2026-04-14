namespace AttendVisionReportsApi.DTOs
{
    public record IssueRowResponse(
        string Date, string TimeOf, string Person,
        string EmployeeId, string Department, string IssueType);

    public record ClockingRowResponse(
        string Date, string Person, string EmployeeId,
        string Department, string AccessTime,
        string AttendanceStatus, string AuthenticationResult);

    public record TimesheetRowResponse(
        string Person, string EmployeeId, string Department,
        string Date, string FirstEntry, string LastEntry,
        double HoursWorked, double BreakHours);
}
