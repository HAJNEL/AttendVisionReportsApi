using System.Globalization;
using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using AttendVisionReportsApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class TimeManagementService(AppDbContext context) : ITimeManagementService
    {
        public async Task<IEnumerable<DaySummaryRow>> GetDaySummaryAsync(
            string date, Guid? departmentId, string? employeeId, Guid userId)
        {
            var day = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var allowedDepts = await GetAllowedDepartmentNamesAsync(userId, departmentId);
            var config = await GetConfigEntityAsync(userId);

            var query = context.AccessRecords.Where(r =>
                r.AccessDate == day &&
                !string.IsNullOrEmpty(r.EmployeeId) &&
                !string.IsNullOrEmpty(r.PersonName));

            if (allowedDepts != null)
                query = query.Where(r => r.Department != null && allowedDepts.Contains(r.Department));

            if (!string.IsNullOrEmpty(employeeId))
                query = query.Where(r => r.EmployeeId == employeeId);

            var records = await query.ToListAsync();

            return records
                .GroupBy(r => new { r.EmployeeId, r.PersonName, r.Department })
                .Select(g =>
                {
                    var ordered = g.OrderBy(r => r.AccessTime).ToList();
                    return new DaySummaryRow(
                        g.Key.EmployeeId ?? "",
                        g.Key.PersonName ?? "",
                        g.Key.Department ?? "",
                        ordered.First().AccessTime.ToString("HH:mm"),
                        ordered.Last().AccessTime.ToString("HH:mm"),
                        g.Count(),
                        DetectIssues(ordered, config).Count
                    );
                })
                .OrderBy(r => r.PersonName)
                .ToList();
        }

        public async Task<IEnumerable<AccessRecordDto>> GetUserRecordsAsync(string date, string employeeId, Guid userId)
        {
            var day = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var records = await context.AccessRecords
                .Where(r => r.AccessDate == day && r.EmployeeId == employeeId)
                .OrderBy(r => r.AccessTime)
                .ToListAsync();
            return records.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<TimeManagementIssue>> GetUserIssuesAsync(string date, string employeeId, Guid userId)
        {
            var day = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var records = await context.AccessRecords
                .Where(r => r.AccessDate == day && r.EmployeeId == employeeId)
                .OrderBy(r => r.AccessTime)
                .ToListAsync();
            var config = await GetConfigEntityAsync(userId);
            return DetectIssues(records, config);
        }

        public async Task<AccessRecordDto> CreateAccessRecordAsync(CreateAccessRecordDto dto)
        {
            var day = DateOnly.ParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var time = ParseTime(dto.Time);
            var record = new AccessRecord
            {
                EmployeeId = dto.EmployeeId,
                PersonName = dto.PersonName,
                Department = dto.Department,
                AccessDate = day,
                AccessTime = time,
                AccessDatetime = DateTime.SpecifyKind(day.ToDateTime(time), DateTimeKind.Utc),
                AttendanceStatus = dto.AttendanceStatus,
                AuthenticationResult = "Success",
                CheckIn = dto.AttendanceStatus == "check_in",
                Failed = false
            };
            context.AccessRecords.Add(record);
            await context.SaveChangesAsync();
            return MapToDto(record);
        }

        public async Task<AccessRecordDto> UpdateAccessRecordAsync(long id, string time, string attendanceStatus)
        {
            var record = await context.AccessRecords.FindAsync(id)
                ?? throw new KeyNotFoundException($"Access record {id} not found.");
            var t = ParseTime(time);
            record.AccessTime = t;
            record.AccessDatetime = DateTime.SpecifyKind(record.AccessDate.ToDateTime(t), DateTimeKind.Utc);
            record.AttendanceStatus = attendanceStatus;
            record.CheckIn = attendanceStatus == "check_in";
            await context.SaveChangesAsync();
            return MapToDto(record);
        }

        public async Task DeleteAccessRecordAsync(long id)
        {
            var record = await context.AccessRecords.FindAsync(id)
                ?? throw new KeyNotFoundException($"Access record {id} not found.");
            context.AccessRecords.Remove(record);
            await context.SaveChangesAsync();
        }

        public async Task<AutoFixPreviewResponse> GetAutoFixPreviewAsync(string date, string employeeId, Guid userId)
        {
            var day = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var records = await context.AccessRecords
                .Where(r => r.AccessDate == day && r.EmployeeId == employeeId)
                .OrderBy(r => r.AccessTime)
                .ToListAsync();
            var config = await GetConfigEntityAsync(userId);
            var issues = DetectIssues(records, config);
            var actions = BuildAutoFixActions(records, issues, config);
            return new AutoFixPreviewResponse(issues, actions);
        }

        public async Task ApplyAutoFixAsync(AutoFixApplyRequest request)
        {
            var day = DateOnly.ParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            foreach (var action in request.Actions)
            {
                switch (action.ActionType)
                {
                    case "add_record" when action.Time != null && action.AttendanceStatus != null:
                    {
                        var t = ParseTime(action.Time);
                        context.AccessRecords.Add(new AccessRecord
                        {
                            EmployeeId = request.EmployeeId,
                            PersonName = request.PersonName,
                            Department = request.Department,
                            AccessDate = day,
                            AccessTime = t,
                            AccessDatetime = DateTime.SpecifyKind(day.ToDateTime(t), DateTimeKind.Utc),
                            AttendanceStatus = action.AttendanceStatus,
                            AuthenticationResult = "Success",
                            CheckIn = action.AttendanceStatus == "check_in",
                            Failed = false
                        });
                        break;
                    }
                    case "update_record" when action.RecordId.HasValue && action.Time != null:
                    {
                        var rec = await context.AccessRecords.FindAsync(action.RecordId.Value);
                        if (rec != null)
                        {
                            var t = ParseTime(action.Time);
                            rec.AccessTime = t;
                            rec.AccessDatetime = DateTime.SpecifyKind(day.ToDateTime(t), DateTimeKind.Utc);
                        }
                        break;
                    }
                    case "delete_record" when action.RecordId.HasValue:
                    case "delete" when action.RecordId.HasValue:
                    {
                        var rec = await context.AccessRecords.FindAsync(action.RecordId.Value);
                        if (rec != null) context.AccessRecords.Remove(rec);
                        break;
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static List<TimeManagementIssue> DetectIssues(List<AccessRecord> records, TimeManagementConfig config)
        {
            var issues = new List<TimeManagementIssue>();
            if (!records.Any()) return issues;

            var statuses = records.Select(r => r.AttendanceStatus?.ToLower()).ToList();

            if (config.DetectMissingCheckIn && !statuses.Contains("check_in"))
                issues.Add(new TimeManagementIssue("missing_check_in", "No check-in record found for this day.", null));

            if (config.DetectMissingCheckOut && !statuses.Contains("check_out"))
                issues.Add(new TimeManagementIssue("missing_check_out", "No check-out record found for this day.", null));

            if (config.DetectMissingBreak)
            {
                var breakOuts = records.Where(r => r.AttendanceStatus?.ToLower() == "break_out").OrderBy(r => r.AccessTime).ToList();
                var breakIns = records.Where(r => r.AttendanceStatus?.ToLower() == "break_in").OrderBy(r => r.AccessTime).ToList();

                if (breakOuts.Count > breakIns.Count)
                    issues.Add(new TimeManagementIssue("missing_break_in",
                        $"Break-out without matching break-in ({breakOuts.Count - breakIns.Count} occurrence(s)).",
                        breakOuts.Last().AccessTime.ToString("HH:mm")));
                else if (breakIns.Count > breakOuts.Count)
                    issues.Add(new TimeManagementIssue("missing_break_out",
                        $"Break-in without matching break-out ({breakIns.Count - breakOuts.Count} occurrence(s)).",
                        breakIns.First().AccessTime.ToString("HH:mm")));
            }

            if (config.DetectDuplicates)
            {
                for (int i = 1; i < records.Count; i++)
                {
                    var prev = records[i - 1].AttendanceStatus?.ToLower();
                    var curr = records[i].AttendanceStatus?.ToLower();
                    if (!string.IsNullOrEmpty(prev) && prev == curr)
                        issues.Add(new TimeManagementIssue("duplicate_consecutive",
                            $"Consecutive '{prev}' records at {records[i - 1].AccessTime:HH:mm} and {records[i].AccessTime:HH:mm}.",
                            records[i].AccessTime.ToString("HH:mm")));
                }
            }

            return issues;
        }

        private static List<AutoFixAction> BuildAutoFixActions(
            List<AccessRecord> records, List<TimeManagementIssue> issues, TimeManagementConfig config)
        {
            var actions = new List<AutoFixAction>();
            var breakMinutes = config.BreakDefaultMinutes;
            foreach (var issue in issues)
            {
                switch (issue.IssueType)
                {
                    case "missing_check_in":
                    {
                        var first = records.OrderBy(r => r.AccessTime).FirstOrDefault();
                        var suggested = first != null ? OffsetTime(first.AccessTime, -config.CheckInOffsetMinutes) : "08:00";
                        var atStr = first != null ? first.AccessTime.ToString("HH:mm") : "unknown";
                        actions.Add(new AutoFixAction("add_record", null, "check_in", suggested,
                            $"Add check-in at {suggested} ({config.CheckInOffsetMinutes} min before first event at {atStr})."));
                        break;
                    }
                    case "missing_check_out":
                    {
                        var checkIn = records
                            .Where(r => r.AttendanceStatus?.ToLower() == "check_in")
                            .OrderBy(r => r.AccessTime).FirstOrDefault();
                        if (checkIn != null)
                        {
                            var workdayMins = (int)Math.Round(config.WorkdayHours * 60);
                            var checkInMins = (int)checkIn.AccessTime.ToTimeSpan().TotalMinutes;
                            var doubleShift = config.EnableDoubleShift && records.Any(r =>
                                r.AttendanceStatus?.ToLower() != "check_in" &&
                                (int)r.AccessTime.ToTimeSpan().TotalMinutes > checkInMins + workdayMins);
                            var hoursMins = (int)Math.Round((doubleShift ? config.DoubleShiftHours : config.WorkdayHours) * 60);
                            var suggested = OffsetTime(checkIn.AccessTime, hoursMins);
                            var label = doubleShift ? "double shift" : "standard day";
                            actions.Add(new AutoFixAction("add_record", null, "check_out", suggested,
                                $"Add check-out at {suggested} ({hoursMins / 60}h after check-in at {checkIn.AccessTime:HH:mm} — {label})."));
                        }
                        else
                        {
                            var last = records.OrderBy(r => r.AccessTime).LastOrDefault();
                            var suggested = last != null ? OffsetTime(last.AccessTime, config.CheckOutOffsetMinutes) : "17:00";
                            var atStr = last != null ? last.AccessTime.ToString("HH:mm") : "unknown";
                            actions.Add(new AutoFixAction("add_record", null, "check_out", suggested,
                                $"Add check-out at {suggested} ({config.CheckOutOffsetMinutes} min after last event at {atStr})."));
                        }
                        break;
                    }
                    case "missing_break_in":
                    {
                        var lastBreakOut = records
                            .Where(r => r.AttendanceStatus?.ToLower() == "break_out")
                            .OrderBy(r => r.AccessTime).LastOrDefault();
                        if (lastBreakOut != null)
                        {
                            var suggested = OffsetTime(lastBreakOut.AccessTime, breakMinutes);
                            actions.Add(new AutoFixAction("add_record", null, "break_in", suggested,
                                $"Add break-in at {suggested} ({breakMinutes} min after break-out at {lastBreakOut.AccessTime:HH:mm})."));
                        }
                        break;
                    }
                    case "missing_break_out":
                    {
                        var firstBreakIn = records
                            .Where(r => r.AttendanceStatus?.ToLower() == "break_in")
                            .OrderBy(r => r.AccessTime).FirstOrDefault();
                        if (firstBreakIn != null)
                        {
                            var suggested = OffsetTime(firstBreakIn.AccessTime, -breakMinutes);
                            actions.Add(new AutoFixAction("add_record", null, "break_out", suggested,
                                $"Add break-out at {suggested} ({breakMinutes} min before break-in at {firstBreakIn.AccessTime:HH:mm})."));
                        }
                        break;
                    }
                    case "duplicate_consecutive":
                    {
                        var dup = BuildDuplicateDeleteAction(records, issue, config);
                        if (dup != null && !actions.Any(a => a.RecordId == dup.RecordId)) actions.Add(dup);
                        break;
                    }
                }
            }
            return actions;
        }

        private static AutoFixAction? BuildDuplicateDeleteAction(
            List<AccessRecord> records, TimeManagementIssue issue, TimeManagementConfig config)
        {
            // issue.RelatedTime is the time of the second (later) record in the consecutive pair
            if (string.IsNullOrEmpty(issue.RelatedTime)) return null;
            var ordered = records.OrderBy(r => r.AccessTime).ToList();
            for (int i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];
                if (curr.AccessTime.ToString("HH:mm") != issue.RelatedTime) continue;
                var s1 = prev.AttendanceStatus?.ToLower();
                var s2 = curr.AttendanceStatus?.ToLower();
                if (string.IsNullOrEmpty(s1) || s1 != s2) continue;

                var toRemove = curr;
                if (config.DuplicateKeepStrategy == "remove_failed_then_second")
                {
                    var prevWrong = IsAuthFailed(prev);
                    var currWrong = IsAuthFailed(curr);
                    if (prevWrong && !currWrong) toRemove = prev;
                    else toRemove = curr; // both ok or both wrong → remove the second
                }

                var reason = IsAuthFailed(toRemove) ? "failed authentication" : "duplicate entry";
                return new AutoFixAction("delete", toRemove.Id, toRemove.AttendanceStatus,
                    toRemove.AccessTime.ToString("HH:mm"),
                    $"Remove {toRemove.AttendanceStatus} at {toRemove.AccessTime:HH:mm} ({reason}).");
            }
            return null;
        }

        private static bool IsAuthFailed(AccessRecord r) =>
            r.Failed == true ||
            (!string.IsNullOrEmpty(r.AuthenticationResult) &&
             !r.AuthenticationResult.Equals("success", StringComparison.OrdinalIgnoreCase));

        private static string OffsetTime(TimeOnly t, int minutes)
        {
            var total = (int)t.ToTimeSpan().TotalMinutes + minutes;
            total = Math.Max(0, Math.Min(total, 23 * 60 + 59));
            return $"{total / 60:D2}:{total % 60:D2}";
        }

        private static TimeOnly ParseTime(string time)
        {
            string[] formats = ["HH:mm", "H:mm", "HH:mm:ss"];
            if (TimeOnly.TryParseExact(time, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                return t;
            throw new ArgumentException($"Invalid time format: {time}");
        }

        private async Task<List<string>?> GetAllowedDepartmentNamesAsync(Guid userId, Guid? filterDepartmentId)
        {
            var userDeptIds = await context.DepartmentUsers
                .Where(du => du.UserId == userId)
                .Select(du => du.DepartmentId)
                .ToListAsync();

            if (!userDeptIds.Any()) return null;

            if (filterDepartmentId.HasValue)
            {
                if (!userDeptIds.Contains(filterDepartmentId.Value)) return [];
                return await context.Departments
                    .Where(d => d.Id == filterDepartmentId.Value)
                    .Select(d => d.DepartmentName)
                    .Where(n => n != null)
                    .ToListAsync()!;
            }

            return await context.Departments
                .Where(d => userDeptIds.Contains(d.Id))
                .Select(d => d.DepartmentName)
                .Where(n => n != null)
                .ToListAsync()!;
        }

        // ── Company config ────────────────────────────────────────────────────

        public async Task<TimeManagementConfigDto> GetConfigAsync(Guid userId)
        {
            var entity = await GetConfigEntityAsync(userId);
            return MapConfigToDto(entity);
        }

        public async Task<TimeManagementConfigDto> SaveConfigAsync(TimeManagementConfigDto dto, Guid userId)
        {
            var companyId = await ResolveCompanyIdAsync(userId)
                ?? throw new InvalidOperationException("No company is linked to the current user.");

            var entity = await context.TimeManagementConfigs
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (entity == null)
            {
                entity = new TimeManagementConfig { Id = Guid.NewGuid(), CompanyId = companyId };
                context.TimeManagementConfigs.Add(entity);
            }

            entity.DetectMissingCheckIn = dto.DetectMissingCheckIn;
            entity.DetectMissingCheckOut = dto.DetectMissingCheckOut;
            entity.DetectMissingBreak = dto.DetectMissingBreak;
            entity.DetectDuplicates = dto.DetectDuplicates;
            entity.EnableDoubleShift = dto.EnableDoubleShift;
            entity.WorkdayHours = dto.WorkdayHours;
            entity.DoubleShiftHours = dto.DoubleShiftHours;
            entity.BreakDefaultMinutes = dto.BreakDefaultMinutes;
            entity.CheckInOffsetMinutes = dto.CheckInOffsetMinutes;
            entity.CheckOutOffsetMinutes = dto.CheckOutOffsetMinutes;
            entity.DuplicateKeepStrategy = string.IsNullOrWhiteSpace(dto.DuplicateKeepStrategy)
                ? "remove_failed_then_second" : dto.DuplicateKeepStrategy;
            entity.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return MapConfigToDto(entity);
        }

        private async Task<TimeManagementConfig> GetConfigEntityAsync(Guid userId)
        {
            var companyId = await ResolveCompanyIdAsync(userId);
            if (companyId == null) return new TimeManagementConfig { CompanyId = Guid.Empty };

            var entity = await context.TimeManagementConfigs
                .FirstOrDefaultAsync(c => c.CompanyId == companyId.Value);
            return entity ?? new TimeManagementConfig { CompanyId = companyId.Value };
        }

        private async Task<Guid?> ResolveCompanyIdAsync(Guid userId)
        {
            var deptIds = await context.DepartmentUsers
                .Where(du => du.UserId == userId)
                .Select(du => du.DepartmentId)
                .ToListAsync();

            if (deptIds.Count == 0) return null;

            return await context.Departments
                .Where(d => deptIds.Contains(d.Id) && d.CompanyId != null)
                .Select(d => d.CompanyId)
                .FirstOrDefaultAsync();
        }

        private static TimeManagementConfigDto MapConfigToDto(TimeManagementConfig c) => new(
            c.CompanyId == Guid.Empty ? null : c.CompanyId,
            c.DetectMissingCheckIn,
            c.DetectMissingCheckOut,
            c.DetectMissingBreak,
            c.DetectDuplicates,
            c.EnableDoubleShift,
            c.WorkdayHours,
            c.DoubleShiftHours,
            c.BreakDefaultMinutes,
            c.CheckInOffsetMinutes,
            c.CheckOutOffsetMinutes,
            c.DuplicateKeepStrategy
        );

        private static AccessRecordDto MapToDto(AccessRecord r) => new(
            r.Id,
            r.EmployeeId ?? "",
            r.PersonName ?? "",
            r.Department ?? "",
            r.AccessDate.ToString("yyyy-MM-dd"),
            r.AccessTime.ToString("HH:mm"),
            r.AttendanceStatus,
            r.AuthenticationResult,
            r.Failed
        );
    }
}
