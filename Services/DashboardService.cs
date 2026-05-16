using AttendVisionReportsApi.DTOs;
using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Services
{
      using AttendVisionReportsApi.Data;
      using AttendVisionReportsApi.Models;
      using Microsoft.EntityFrameworkCore;
      using System.Globalization;
      public class DashboardService : IDashboardService
      {
        private readonly AppDbContext db;
        public DashboardService(AppDbContext db)
        {
          this.db = db;
        }


        public async Task<DashboardKpisResponse> GetKpisAsync(string dateFrom, string dateTo, string? department, string? employee)
            => await GetKpisAsync(dateFrom, dateTo, department, employee, null);

        public async Task<DashboardKpisResponse> GetKpisAsync(string dateFrom, string dateTo, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
            var totalEmployeesDetails = await GetTotalEmployeesKpiDetailsAsync(dateFrom, dateTo, department, employee, user);
            var checkinsTodayDetails = await GetCheckinsTodayKpiDetailsAsync(dateFrom, dateTo, department, employee, user);
            var onSiteDetails = await GetOnSiteKpiDetailsAsync(dateFrom, dateTo, department, employee, user);
            var onBreakDetails = await GetOnBreakKpiDetailsAsync(dateFrom, dateTo, department, employee, user);

            return new DashboardKpisResponse(
                totalEmployeesDetails,
                checkinsTodayDetails,
                onSiteDetails,
                onBreakDetails
            );
        }

        // Private KPI detail methods
        private async Task<List<EmployeeKpiDetail>> GetTotalEmployeesKpiDetailsAsync(string dateFrom, string dateTo, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
            var dateFromVal = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dateToVal = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var dateToFinal = dateToVal < today ? dateToVal : today;

            var depts = await db.Departments.ToListAsync();
            var deptLookup = depts.ToDictionary(d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

            var query = db.AccessRecords.AsQueryable();
            query = query.Where(x => x.AccessDate >= dateFromVal && x.AccessDate <= dateToFinal);
            var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
            if (allowedDepartments != null)
                query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
            if (!string.IsNullOrEmpty(employee))
              query = query.Where(x => x.EmployeeId == employee);

            var rangeRecords = await query
                .Where(x => x.EmployeeId != null)
                .Select(x => new { x.EmployeeId, x.PersonName, x.Department, x.AccessDatetime, x.AccessTime, x.AttendanceStatus })
                .ToListAsync();

            var rangeGrouped = rangeRecords
                .GroupBy(x => x.EmployeeId)
                .Select(g => {
                    var events = g.OrderBy(r => r.AccessDatetime).ToList();
                    var latest = events.LastOrDefault();
                    var firstCheckIn = events.FirstOrDefault(r => r.AttendanceStatus == "check_in");
                    var lastCheckOut = events.LastOrDefault(r => r.AttendanceStatus == "check_out");

                    string? checkInTime = firstCheckIn != null ? events.FirstOrDefault(r => r.AttendanceStatus == "check_in")?.AccessTime.ToString("HH:mm:ss") : null;
                    string? checkOutTime = lastCheckOut != null ? events.LastOrDefault(r => r.AttendanceStatus == "check_out")?.AccessTime.ToString("HH:mm:ss") : null;

                    var deptName = latest?.Department ?? "Unknown";
                    deptLookup.TryGetValue(deptName, out var deptModel);

                    return new EmployeeKpiDetail(
                        g.Key,
                        deptName,
                        latest?.PersonName ?? g.Key,
                        checkInTime,
                        checkOutTime,
                        latest?.AttendanceStatus
                    );
                })
                .ToList();

            return rangeGrouped;
        }

        private async Task<List<CheckInKpiDetail>> GetCheckinsTodayKpiDetailsAsync(string dateFrom, string dateTo, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
            var dateFromVal = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dateToVal = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var dateToFinal = dateToVal < today ? dateToVal : today;

            var depts = await db.Departments.ToListAsync();
            var deptLookup = depts.ToDictionary(d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

            var query = db.AccessRecords.AsQueryable();
            query = query.Where(x => x.AccessDate >= dateFromVal && x.AccessDate <= dateToFinal);
            var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
            if (allowedDepartments != null)
                query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
            if (!string.IsNullOrEmpty(employee))
              query = query.Where(x => x.EmployeeId == employee);

            var rangeRecords = await query
                .Where(x => x.EmployeeId != null)
                .Select(x => new { x.EmployeeId, x.PersonName, x.Department, x.AccessDatetime, x.AccessTime, x.AttendanceStatus })
                .ToListAsync();

            var rangeGrouped = rangeRecords
                .GroupBy(x => x.EmployeeId)
                .Select(g => {
                    var events = g.OrderBy(r => r.AccessDatetime).ToList();
                    var latest = events.LastOrDefault();
                    var firstCheckIn = events.FirstOrDefault(r => r.AttendanceStatus == "check_in");
                    string? checkInTime = firstCheckIn != null ? events.FirstOrDefault(r => r.AttendanceStatus == "check_in")?.AccessTime.ToString("HH:mm:ss") : null;
                    return new {
                        EmployeeId = g.Key,
                        DepartmentName = latest?.Department ?? "Unknown",
                        FullName = latest?.PersonName ?? g.Key,
                        HasCheckIn = firstCheckIn != null,
                        CheckInTime = checkInTime
                    };
                })
                .Where(d => d.HasCheckIn)
                .Select(d => new CheckInKpiDetail(
                    d.EmployeeId,
                    d.DepartmentName,
                    d.FullName,
                    d.CheckInTime,
                    null,
                    null
                ))
                .ToList();

            return rangeGrouped;
        }

        private async Task<List<OnSiteKpiDetail>> GetOnSiteKpiDetailsAsync(string dateFrom, string dateTo, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
            var dateToVal = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var dateToFinal = dateToVal < today ? dateToVal : today;
            var now = DateTime.UtcNow;

            var onSiteQuery = db.AccessRecords.AsQueryable();
            onSiteQuery = onSiteQuery.Where(x => x.AccessDate == dateToFinal && x.EmployeeId != null);
            var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
            if (allowedDepartments != null)
                onSiteQuery = onSiteQuery.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
            if (!string.IsNullOrEmpty(employee))
              onSiteQuery = onSiteQuery.Where(x => x.EmployeeId == employee);

            var currentRecords = await onSiteQuery
                .Select(x => new { x.EmployeeId, x.PersonName, x.Department, x.AccessDatetime, x.AttendanceStatus })
                .ToListAsync();

            var currentGrouped = currentRecords
                .GroupBy(x => x.EmployeeId)
                .Select(g => {
                    var events = g.OrderBy(r => r.AccessDatetime).ToList();
                    var latest = events.LastOrDefault();
                    var firstCheckIn = events.FirstOrDefault(e => e.AttendanceStatus == "check_in");
                    var lastCheckOut = events.LastOrDefault(e => e.AttendanceStatus == "check_out");

                    DateTime endTime;
                    if (lastCheckOut != null)
                        endTime = lastCheckOut.AccessDatetime;
                    else if (events.Count > 0 && events.Last().AccessDatetime.Date == now.Date)
                        endTime = now;
                    else
                        endTime = events.LastOrDefault()?.AccessDatetime ?? now;

                    double totalBreakMs = 0;
                    DateTime? breakStart = null;
                    foreach (var e in events)
                    {
                        if (e.AttendanceStatus == "break_out")
                        {
                            breakStart = e.AccessDatetime;
                        }
                        else if (e.AttendanceStatus == "break_in" && breakStart != null)
                        {
                            totalBreakMs += (e.AccessDatetime - breakStart.Value).TotalMilliseconds;
                            breakStart = null;
                        }
                    }
                    if (breakStart != null)
                    {
                        totalBreakMs += (now - breakStart.Value).TotalMilliseconds;
                    }

                    double totalWorkedMs = 0;
                    if (firstCheckIn != null)
                    {
                        totalWorkedMs = (endTime - firstCheckIn.AccessDatetime).TotalMilliseconds - totalBreakMs;
                        if (totalWorkedMs < 0) totalWorkedMs = 0;
                    }

                    var lastBreakIn = events.LastOrDefault(e => e.AttendanceStatus == "break_in");
                    string timeSinceLastBreak = "00:00";
                    if (lastBreakIn != null)
                    {
                        timeSinceLastBreak = (now - lastBreakIn.AccessDatetime).ToString(@"hh\:mm");
                    }
                    else if (firstCheckIn != null)
                    {
                        timeSinceLastBreak = (now - firstCheckIn.AccessDatetime).ToString(@"hh\:mm");
                    }

                    return new {
                        EmployeeId = g.Key,
                        FullName = latest?.PersonName ?? g.Key,
                        DepartmentName = latest?.Department ?? "Unknown",
                        LatestStatus = latest?.AttendanceStatus,
                        TotalWorked = TimeSpan.FromMilliseconds(totalWorkedMs).ToString(@"hh\:mm"),
                        TimeSinceLastBreak = timeSinceLastBreak
                    };
                })
                .ToList();

            var onSiteDetails = currentGrouped
                .Where(d => d.LatestStatus == "check_in" || d.LatestStatus == "break_in")
                .Select(d => new OnSiteKpiDetail(
                    d.EmployeeId, d.DepartmentName, d.FullName, d.TotalWorked, d.TimeSinceLastBreak)).ToList();

            return onSiteDetails;
        }

        private async Task<List<OnBreakKpiDetail>> GetOnBreakKpiDetailsAsync(string dateFrom, string dateTo, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
          // We'll use dateTo as the date for the KPI (like the other KPIs)
          var dateVal = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
          var query = db.AccessRecords.AsQueryable();
          query = query.Where(x => x.AccessDate == dateVal);
          var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
          if (allowedDepartments != null)
            query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
          if (!string.IsNullOrEmpty(employee))
            query = query.Where(x => x.EmployeeId == employee);

          var records = await query
            .Where(x => x.EmployeeId != null || !string.IsNullOrWhiteSpace(x.PersonName))
            .Select(x => new {
              PersonKey = !string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : (x.EmployeeId ?? "Unknown"),
              x.Department,
              x.AccessDatetime,
              x.AccessTime,
              x.AttendanceStatus
            })
            .ToListAsync();

          // Group by employee, get all events for each
          var now = DateTime.UtcNow;
          var result = new List<OnBreakKpiDetail>();
          foreach (var group in records.GroupBy(x => new { x.PersonKey, x.Department }))
          {
            var events = group.OrderBy(e => e.AccessDatetime).ToList();
            // Find the latest event
            var latest = events.LastOrDefault();
            if (latest == null || latest.AttendanceStatus != "break_out")
              continue;

            // Find when the break started (the last break_out not followed by break_in)
            DateTime? breakStart = null;
            TimeOnly? breakAccessTime = null;
            for (int i = events.Count - 1; i >= 0; i--)
            {
              if (events[i].AttendanceStatus == "break_out")
              {
                // Check if there is a break_in after this break_out
                bool hasBreakIn = events.Skip(i + 1).Any(e => e.AttendanceStatus == "break_in");
                if (!hasBreakIn)
                {
                  breakStart = events[i].AccessDatetime;
                  breakAccessTime = events[i].AccessTime;
                  break;
                }
              }
            }
            if (breakStart == null || breakAccessTime == null)
              continue;

            // Break Started: the actual access_time from the access_records row
            var breakStarted = breakAccessTime.Value.ToString("HH:mm");
            var totalTimeOnBreak = (now - breakStart.Value).ToString(@"hh\:mm");

            result.Add(new OnBreakKpiDetail(
              group.Key.PersonKey,
              group.Key.Department ?? "Unknown",
              group.Key.PersonKey,
              breakStarted,
              totalTimeOnBreak
            ));
          }
          return result;
        }

        public async Task<IEnumerable<dynamic>> GetHourlyTrafficAsync(string date, string? department)
          => await GetHourlyTrafficAsync(date, department, null);

        public async Task<IEnumerable<dynamic>> GetHourlyTrafficAsync(string date, string? department, System.Security.Claims.ClaimsPrincipal? user)
        {
          var dateVal = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

          var query = db.AccessRecords.AsQueryable();
          query = query.Where(x => x.AccessDate == dateVal);
          var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
          if (allowedDepartments != null)
            query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));

          var result = await query
            .GroupBy(x => x.AccessTime)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
              label = g.Key.ToString("HH:mm"),
              count = g.Count(),
              names = string.Join(", ", g.OrderBy(x => x.AccessTime)
                .Select(x => !string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : (x.EmployeeId ?? "Unknown")))
            })
            .ToListAsync();
          return result;
        }

        public async Task<IEnumerable<dynamic>> GetMonthlyAttendanceAsync(string? department, string? employee)
          => await GetMonthlyAttendanceAsync(department, employee, null);

        public async Task<IEnumerable<dynamic>> GetMonthlyAttendanceAsync(string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
          var monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

          var query = db.AccessRecords.AsQueryable();
          query = query.Where(x => x.AccessDate >= monthStart && x.EmployeeId != null);
          var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
          if (allowedDepartments != null)
            query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
          if (!string.IsNullOrEmpty(employee))
            query = query.Where(x => x.EmployeeId == employee);

          var result = await query
            .GroupBy(x => x.AccessDate)
            .OrderBy(g => g.Key)
            .Select(g => new {
              label = g.Key.ToString("yyyy-MM-dd"),
              count = g.Select(x => x.EmployeeId).Distinct().Count()
            })
            .ToListAsync();
          return result;
        }

        public async Task<IEnumerable<dynamic>> GetDeptBreakdownAsync(string? department)
          => await GetDeptBreakdownAsync(department, null);

        public async Task<IEnumerable<dynamic>> GetDeptBreakdownAsync(string? department, System.Security.Claims.ClaimsPrincipal? user)
        {

          var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
          var query = db.AccessRecords.AsQueryable();
          query = query.Where(x => x.AccessDate == today && x.AttendanceStatus == "check_in" && x.EmployeeId != null);
          var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
          if (allowedDepartments != null)
            query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));

          var result = await query
            .GroupBy(x => x.Department)
            .Select(g => new {
              label = g.Key ?? "Unknown",
              count = g.Select(x => x.EmployeeId).Distinct().Count()
            })
            .OrderByDescending(x => x.count)
            .Take(10)
            .ToListAsync();
          return result;
        }

        public async Task<IEnumerable<dynamic>> GetMonthlyTrafficAsync(int year, int month, string? department, string? employee)
          => await GetMonthlyTrafficAsync(year, month, department, employee, null);

        public async Task<IEnumerable<dynamic>> GetMonthlyTrafficAsync(int year, int month, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
          var dateStart = new DateOnly(year, month, 1);
          var dateEnd = dateStart.AddMonths(1);

          var query = db.AccessRecords.AsQueryable();
          query = query.Where(x => x.AccessDate >= dateStart && x.AccessDate < dateEnd);
          var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
          if (allowedDepartments != null)
            query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
          if (!string.IsNullOrEmpty(employee))
            query = query.Where(x => x.EmployeeId == employee);

          var result = await query
            .GroupBy(x => x.AccessDate)
            .OrderBy(g => g.Key)
            .Select(g => new {
              label = g.Key.ToString("yyyy-MM-dd"),
              count = g.Count()
            })
            .ToListAsync();
          return result;
        }

        public async Task<IEnumerable<dynamic>> GetYearlyTrafficAsync(int year, string? department, string? employee)
          => await GetYearlyTrafficAsync(year, department, employee, null);

        public async Task<IEnumerable<dynamic>> GetYearlyTrafficAsync(int year, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
          var dateStart = new DateOnly(year, 1, 1);
          var dateEnd = dateStart.AddYears(1);

          var query = db.AccessRecords.AsQueryable();
          query = query.Where(x => x.AccessDate >= dateStart && x.AccessDate < dateEnd);
          var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
          if (allowedDepartments != null)
            query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
          if (!string.IsNullOrEmpty(employee))
            query = query.Where(x => x.EmployeeId == employee);

          var result = await query
            .GroupBy(x => new { x.AccessDate.Year, x.AccessDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new {
              label = g.Key.Year.ToString("D4") + "-" + g.Key.Month.ToString("D2"),
              count = g.Count()
            })
            .ToListAsync();
          return result;
        }

        public async Task<IEnumerable<dynamic>> GetDayEventsAsync(string date, string? department, string? employee)
          => await GetDayEventsAsync(date, department, employee, null);

        public async Task<IEnumerable<dynamic>> GetDayEventsAsync(string date, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
          var dateVal = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

          var query = db.AccessRecords.AsQueryable();
          query = query.Where(x => x.AccessDate == dateVal);
          var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
          if (allowedDepartments != null)
            query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
          if (!string.IsNullOrEmpty(employee))
            query = query.Where(x => x.EmployeeId == employee);

          // Group by 15-minute intervals and attendance_status
          var records = await query.ToListAsync();
          var result = records
            .GroupBy(x => new {
              label = new TimeSpan(x.AccessTime.Hour, (x.AccessTime.Minute / 15) * 15, 0).ToString(@"hh\:mm"),
              status = string.IsNullOrWhiteSpace(x.AttendanceStatus) ? "unknown" : x.AttendanceStatus
            })
            .OrderBy(g => g.Key.label).ThenBy(g => g.Key.status)
            .Select(g => new {
              label = g.Key.label,
              status = g.Key.status,
              count = g.Count(),
              names = string.Join(", ", g.OrderBy(x => x.AccessTime)
                .Select(x => !string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : (x.EmployeeId ?? "Unknown")))
            })
            .ToList();
          return result;
        }

        public async Task<IEnumerable<DayPersonRowResponse>> GetDayPeopleAsync(string date, string? department, string? employee)
          => await GetDayPeopleAsync(date, department, employee, null);

        public async Task<IEnumerable<DayPersonRowResponse>> GetDayPeopleAsync(string date, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
            var dateVal = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var query = db.AccessRecords.AsQueryable();
            query = query.Where(x => x.AccessDate == dateVal);
            var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
            if (allowedDepartments != null)
              query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
            if (!string.IsNullOrEmpty(employee))
              query = query.Where(x => x.EmployeeId == employee);

            var records = await query
              .Select(x => new {
                Person = !string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : (x.EmployeeId ?? "Unknown"),
                Department = x.Department ?? "Unknown",
                AccessDatetime = x.AccessDatetime,
                AccessTime = x.AccessTime,
                AttendanceStatus = string.IsNullOrWhiteSpace(x.AttendanceStatus) ? "unknown" : x.AttendanceStatus
              })
              .OrderBy(x => x.Person).ThenBy(x => x.AccessDatetime)
              .ToListAsync();

            var grouped = records.GroupBy(x => new { x.Person, x.Department });
            var result = new List<DayPersonRowResponse>();
            foreach (var group in grouped)
            {
                var events = group.ToList();
                var eventCount = events.Count;
                var firstTime = events.Min(e => e.AccessTime).ToString("HH:mm");
                var lastTime = events.Max(e => e.AccessTime).ToString("HH:mm");
                var lastStatus = events.OrderByDescending(e => e.AccessDatetime).FirstOrDefault()?.AttendanceStatus ?? "unknown";

                // Calculate gross hours (check_in to check_out)
                var firstCheckIn = events.Where(e => e.AttendanceStatus == "check_in").OrderBy(e => e.AccessDatetime).FirstOrDefault();
                var lastCheckOut = events.Where(e => e.AttendanceStatus == "check_out").OrderByDescending(e => e.AccessDatetime).FirstOrDefault();
                double grossHours = 0;
                if (firstCheckIn != null && lastCheckOut != null && lastCheckOut.AccessDatetime > firstCheckIn.AccessDatetime)
                {
                  grossHours = (lastCheckOut.AccessDatetime - firstCheckIn.AccessDatetime).TotalHours;
                }

                // Calculate break durations
                double hoursBreak = 0;
                var prevStatus = "";
                DateTime? breakStart = null;
                foreach (var e in events)
                {
                  if (e.AttendanceStatus == "break_out" && prevStatus != "break_out")
                  {
                    breakStart = e.AccessDatetime;
                  }
                  else if (e.AttendanceStatus == "break_in" && breakStart != null)
                  {
                    hoursBreak += (e.AccessDatetime - breakStart.Value).TotalHours;
                    breakStart = null;
                  }
                  prevStatus = e.AttendanceStatus;
                }

                // Compose response
                var hoursWorked = Math.Max(grossHours - hoursBreak, 0);
                result.Add(new DayPersonRowResponse
                {
                  person = group.Key.Person,
                  department = group.Key.Department,
                  event_count = eventCount,
                  first_time = firstTime,
                  last_time = lastTime,
                  last_status = lastStatus,
                  hours_break = hoursBreak,
                  break_time = TimeSpan.FromHours(hoursBreak).ToString(@"hh\:mm"),
                  hours_worked = hoursWorked,
                  worked_time = TimeSpan.FromHours(hoursWorked).ToString(@"hh\:mm")
                });
            }
            return result.OrderByDescending(x => x.event_count).ThenBy(x => x.person).ToList();
        }


        // Helper: get allowed departments for user
        private async Task<List<string>?> GetAllowedDepartmentsAsync(System.Security.Claims.ClaimsPrincipal? user, string? department)
        {
            if (!string.IsNullOrEmpty(department))
                return new List<string> { department };
              if (user == null)
                return null;
              if (!Helpers.ClaimsHelper.TryGetUserId(user, out var userId, logClaims: false))
                return null;
            var departmentIds = await db.DepartmentUsers
                .Where(du => du.UserId == userId)
                .Select(du => du.DepartmentId)
                .ToListAsync();
            if (!departmentIds.Any())
                return new List<string>();
            var departmentNames = await db.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .Select(d => d.DepartmentName)
                .ToListAsync();
            return departmentNames;
        }
    }
}
