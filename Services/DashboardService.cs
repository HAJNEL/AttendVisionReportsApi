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
          /// <summary>
          /// Returns the number of employees currently on break for a given date, optionally filtered by department and employee.
          /// </summary>
          public async Task<int> GetOnBreakNowCountAsync(string date, string? department, string? employee)
          {
            var dateVal = DateOnly.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var query = db.AccessRecords.AsQueryable();
            query = query.Where(x => x.AccessDate == dateVal);
            var allowedDepartments = await GetAllowedDepartmentsAsync(null, department);
            if (allowedDepartments != null)
              query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
            if (!string.IsNullOrEmpty(employee))
              query = query.Where(x => (!string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : x.EmployeeId) == employee);

            // Project to anonymous with employee key, datetime, and status
            var records = await query
              .Where(x => x.EmployeeId != null || !string.IsNullOrWhiteSpace(x.PersonName))
              .Select(x => new {
                PersonKey = !string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : (x.EmployeeId ?? "Unknown"),
                x.AccessDatetime,
                x.AttendanceStatus
              })
              .ToListAsync();

            // Group by employee, get latest event, count those with 'break_out' and not followed by 'break_in'
            var grouped = records
              .GroupBy(x => x.PersonKey)
              .Select(g => g.OrderByDescending(e => e.AccessDatetime).FirstOrDefault())
              .Where(e => e != null && e.AttendanceStatus == "break_out")
              .Count();

            return grouped;
          }

        public async Task<DashboardKpisResponse> GetKpisAsync(string dateFrom, string dateTo, string? department, string? employee)
          => await GetKpisAsync(dateFrom, dateTo, department, employee, null);

        public async Task<DashboardKpisResponse> GetKpisAsync(string dateFrom, string dateTo, string? department, string? employee, System.Security.Claims.ClaimsPrincipal? user)
        {
          var dateFromVal = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
          var dateToVal = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
          var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
          var dateToFinal = dateToVal < today ? dateToVal : today;


          var query = db.AccessRecords.AsQueryable();
          query = query.Where(x => x.AccessDate >= dateFromVal && x.AccessDate <= dateToFinal);
          var allowedDepartments = await GetAllowedDepartmentsAsync(user, department);
          if (allowedDepartments != null)
            query = query.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
          if (!string.IsNullOrEmpty(employee))
            query = query.Where(x => (x.PersonName != null && x.PersonName.Trim() != "") ? x.PersonName == employee : x.EmployeeId == employee);

          var total = await query.Where(x => x.EmployeeId != null).Select(x => x.EmployeeId).Distinct().CountAsync();

          var checkins = await query.Where(x => x.AttendanceStatus == "check_in" && x.EmployeeId != null)
            .Select(x => x.EmployeeId).Distinct().CountAsync();

          // OnSite: employees who checked in but not checked out on dateToFinal

          var onSiteQuery = db.AccessRecords.AsQueryable();
          onSiteQuery = onSiteQuery.Where(x => x.AccessDate == dateToFinal && x.EmployeeId != null);
          if (allowedDepartments != null)
            onSiteQuery = onSiteQuery.Where(x => !string.IsNullOrEmpty(x.Department) && allowedDepartments.Contains(x.Department));
          if (!string.IsNullOrEmpty(employee))
            onSiteQuery = onSiteQuery.Where(x => (x.PersonName != null && x.PersonName.Trim() != "") ? x.PersonName == employee : x.EmployeeId == employee);

          var onSite = await onSiteQuery
            .GroupBy(x => x.EmployeeId)
            .Select(g => new
            {
              HasCheckin = g.Any(r => r.AttendanceStatus == "check_in"),
              HasCheckout = g.Any(r => r.AttendanceStatus == "check_out")
            })
            .CountAsync(x => x.HasCheckin && !x.HasCheckout);

          var failed = await query.Where(x => x.AttendanceStatus == "").CountAsync();

          return new DashboardKpisResponse(total, checkins, onSite, failed);
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
            query = query.Where(x => (!string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : x.EmployeeId) == employee);

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
            query = query.Where(x => (!string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : x.EmployeeId) == employee);

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
            query = query.Where(x => (!string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : x.EmployeeId) == employee);

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
            query = query.Where(x => (!string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : x.EmployeeId) == employee);

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
              query = query.Where(x => (!string.IsNullOrWhiteSpace(x.PersonName) ? x.PersonName : x.EmployeeId) == employee);

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
