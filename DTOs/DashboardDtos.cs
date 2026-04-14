namespace AttendVisionReportsApi.DTOs
{
    public record DashboardKpisResponse(
        long TotalEmployees, long CheckinsToday, long OnSiteNow, long FailedToday);

    public record LabeledCountResponse(string Label, long Count);

    public record DayAccessRowResponse(string Label, long Count, string Names);

    public record DayEventRowResponse(string Label, string Status, long Count, string Names);

    public class DayPersonRowResponse
    {
        public string person { get; set; } = default!;
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
