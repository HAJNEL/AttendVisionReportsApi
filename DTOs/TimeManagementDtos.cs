namespace AttendVisionReportsApi.DTOs
{
    public record DaySummaryRow(
        string EmployeeId,
        string PersonName,
        string Department,
        string? FirstEntry,
        string? LastEntry,
        int TotalRecords,
        int IssueCount
    );

    public record AccessRecordDto(
        long Id,
        string EmployeeId,
        string PersonName,
        string Department,
        string Date,
        string Time,
        string? AttendanceStatus,
        string? AuthenticationResult,
        bool? Failed
    );

    public record CreateAccessRecordDto(
        string EmployeeId,
        string PersonName,
        string Department,
        string Date,
        string Time,
        string AttendanceStatus
    );

    public record UpdateAccessRecordDto(string Time, string AttendanceStatus);

    public record TimeManagementIssue(
        string IssueType,
        string Description,
        string? RelatedTime
    );

    public record AutoFixAction(
        string ActionType,
        long? RecordId,
        string? AttendanceStatus,
        string? Time,
        string Description
    );

    public record AutoFixPreviewResponse(
        IEnumerable<TimeManagementIssue> Issues,
        IEnumerable<AutoFixAction> ProposedActions
    );

    public record AutoFixApplyRequest(
        string EmployeeId,
        string PersonName,
        string Department,
        string Date,
        IEnumerable<AutoFixAction> Actions
    );

    public record TimeManagementConfigDto(
        Guid? CompanyId,
        bool DetectMissingCheckIn,
        bool DetectMissingCheckOut,
        bool DetectMissingBreak,
        bool DetectDuplicates,
        bool EnableDoubleShift,
        decimal WorkdayHours,
        decimal DoubleShiftHours,
        int BreakDefaultMinutes,
        int CheckInOffsetMinutes,
        int CheckOutOffsetMinutes,
        string DuplicateKeepStrategy
    );
}
