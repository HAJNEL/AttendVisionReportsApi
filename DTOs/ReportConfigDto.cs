namespace AttendVisionReportsApi.DTOs
{
    public record ReportConfigDto(
        Guid? CompanyId,
        int? MonthStartDay,
        int? MonthEndDay
    );
}
