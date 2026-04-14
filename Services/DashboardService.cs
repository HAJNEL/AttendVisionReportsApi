using AttendVisionReportsApi.DTOs;
using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Services
{
    public class DashboardService(NpgsqlDataSource dataSource) : IDashboardService
    {

        public async Task<DashboardKpisResponse> GetKpisAsync(string dateFrom, string dateTo, string? department, string? employee)
        {
            using var conn = dataSource.CreateConnection();
            var p = new { dateFrom, dateTo, dept = department, emp = employee };

            var total = await conn.QuerySingleAsync<long>(
                "SELECT COUNT(DISTINCT employee_id) FROM access_records " +
                "WHERE employee_id IS NOT NULL " +
                "AND access_date BETWEEN @dateFrom::date AND LEAST(@dateTo::date, CURRENT_DATE) " +
                "AND (@dept::text IS NULL OR department = @dept) " +
                "AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp)", p);

            var checkins = await conn.QuerySingleAsync<long>(
                "SELECT COUNT(DISTINCT employee_id) FROM access_records " +
                "WHERE access_date BETWEEN @dateFrom::date AND LEAST(@dateTo::date, CURRENT_DATE) " +
                "AND attendance_status = 'check_in' AND employee_id IS NOT NULL " +
                "AND (@dept::text IS NULL OR department = @dept) " +
                "AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp)", p);

            var onSite = await conn.QuerySingleAsync<long>(
                "WITH per_employee AS (" +
                "  SELECT employee_id," +
                "    MAX(CASE WHEN attendance_status = 'check_in'  THEN 1 ELSE 0 END) AS has_checkin," +
                "    MAX(CASE WHEN attendance_status = 'check_out' THEN 1 ELSE 0 END) AS has_checkout" +
                "  FROM access_records" +
                "  WHERE access_date = LEAST(@dateTo::date, CURRENT_DATE)" +
                "    AND employee_id IS NOT NULL" +
                "    AND (@dept::text IS NULL OR department = @dept)" +
                "    AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp)" +
                "  GROUP BY employee_id" +
                ") SELECT COUNT(*) FROM per_employee WHERE has_checkin = 1 AND has_checkout = 0",
                new { dateTo, dept = department, emp = employee });

            var failed = await conn.QuerySingleAsync<long>(
                "SELECT COUNT(*) FROM access_records " +
                "WHERE access_date BETWEEN @dateFrom::date AND LEAST(@dateTo::date, CURRENT_DATE) " +
                "AND attendance_status = '' " +
                "AND (@dept::text IS NULL OR department = @dept) " +
                "AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp)", p);

            return new DashboardKpisResponse(total, checkins, onSite, failed);
        }

        public async Task<IEnumerable<dynamic>> GetHourlyTrafficAsync(string date, string? department)
        {
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(
                "SELECT TO_CHAR(date_trunc('minute', access_time), 'HH24:MI') AS label," +
                "       COUNT(*)::bigint AS count," +
                "       STRING_AGG(COALESCE(NULLIF(TRIM(person_name), ''), employee_id, 'Unknown'), ', ' ORDER BY access_time) AS names " +
                "FROM access_records " +
                "WHERE access_date = @date::date AND (@dept::text IS NULL OR department = @dept) " +
                "GROUP BY date_trunc('minute', access_time) ORDER BY date_trunc('minute', access_time)",
                new { date, dept = department });
        }

        public async Task<IEnumerable<dynamic>> GetMonthlyAttendanceAsync(string? department, string? employee)
        {
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(
                "SELECT access_date::text AS label, COUNT(DISTINCT employee_id)::bigint AS count " +
                "FROM access_records " +
                "WHERE access_date >= date_trunc('month', CURRENT_DATE)::date " +
                "  AND employee_id IS NOT NULL AND (@dept::text IS NULL OR department = @dept) " +
                "  AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp) " +
                "GROUP BY access_date ORDER BY access_date",
                new { dept = department, emp = employee });
        }

        public async Task<IEnumerable<dynamic>> GetDeptBreakdownAsync(string? department)
        {
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(
                "SELECT COALESCE(department, 'Unknown') AS label, COUNT(DISTINCT employee_id)::bigint AS count " +
                "FROM access_records " +
                "WHERE access_date = CURRENT_DATE AND check_in = true " +
                "AND (@dept::text IS NULL OR department = @dept) " +
                "GROUP BY department ORDER BY count DESC LIMIT 10",
                new { dept = department });
        }

        public async Task<IEnumerable<dynamic>> GetMonthlyTrafficAsync(int year, int month, string? department, string? employee)
        {
            var dateStart = $"{year}-{month:D2}-01";
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(
                "SELECT access_date::text AS label, COUNT(*)::bigint AS count " +
                "FROM access_records " +
                "WHERE access_date >= @dateStart::date AND access_date < (@dateStart::date + INTERVAL '1 month') " +
                "AND (@dept::text IS NULL OR department = @dept) " +
                "AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp) " +
                "GROUP BY access_date ORDER BY access_date",
                new { dateStart, dept = department, emp = employee });
        }

        public async Task<IEnumerable<dynamic>> GetYearlyTrafficAsync(int year, string? department, string? employee)
        {
            var dateStart = $"{year}-01-01";
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(
                "SELECT TO_CHAR(access_date, 'YYYY-MM') AS label, COUNT(*)::bigint AS count " +
                "FROM access_records " +
                "WHERE access_date >= @dateStart::date AND access_date < (@dateStart::date + INTERVAL '1 year') " +
                "AND (@dept::text IS NULL OR department = @dept) " +
                "AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp) " +
                "GROUP BY label ORDER BY label",
                new { dateStart, dept = department, emp = employee });
        }

        public async Task<IEnumerable<dynamic>> GetDayEventsAsync(string date, string? department, string? employee)
        {
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(@"
SELECT
  TO_CHAR(
    date_trunc('hour', access_time) +
    (EXTRACT(MINUTE FROM access_time)::int / 15) * INTERVAL '15 minutes',
    'HH24:MI'
  ) AS label,
  COALESCE(attendance_status, 'unknown') AS status,
  COUNT(*)::bigint AS count,
  STRING_AGG(COALESCE(NULLIF(TRIM(person_name), ''), employee_id, 'Unknown'), ', ' ORDER BY access_time) AS names
FROM access_records
WHERE access_date = @date::date
  AND (@dept::text IS NULL OR department = @dept)
  AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp)
GROUP BY 1, 2 ORDER BY 1, 2",
                new { date, dept = department, emp = employee });
        }

        public async Task<IEnumerable<DayPersonRowResponse>> GetDayPeopleAsync(string date, string? department, string? employee)
        {
            using var conn = dataSource.CreateConnection();
            var sql = @"
WITH all_records AS (
  SELECT COALESCE(NULLIF(TRIM(person_name), ''), employee_id, 'Unknown') AS person,
    COALESCE(department, 'Unknown') AS department,
    access_datetime, access_time, attendance_status
  FROM access_records
  WHERE access_date = @date::date AND (@dept::text IS NULL OR department = @dept)
    AND (@emp::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @emp)
),
deduped AS (
  SELECT *, LAG(attendance_status) OVER (PARTITION BY person ORDER BY access_datetime) AS prev_status
  FROM all_records
),
first_breaks AS (
  SELECT person, access_datetime FROM deduped
  WHERE attendance_status = 'break_out' AND (prev_status IS NULL OR prev_status != 'break_out')
),
break_durations AS (
  SELECT fb.person, fb.access_datetime AS break_start,
    (SELECT MIN(ar.access_datetime) FROM all_records ar
     WHERE ar.person = fb.person AND ar.attendance_status = 'break_in' AND ar.access_datetime > fb.access_datetime) AS break_end
  FROM first_breaks fb
),
break_totals AS (
  SELECT person,
    COALESCE(SUM(CASE WHEN break_end IS NOT NULL
      THEN EXTRACT(EPOCH FROM (break_end - break_start)) / 3600.0
      ELSE 0 END), 0)::float8 AS hours_break
  FROM break_durations GROUP BY person
),
day_summary AS (
  SELECT person, department, COUNT(*)::bigint AS event_count,
    COALESCE(MIN(TO_CHAR(access_time, 'HH24:MI')), '')::text AS first_time,
    COALESCE(MAX(TO_CHAR(access_time, 'HH24:MI')), '')::text AS last_time,
    (ARRAY_AGG(COALESCE(attendance_status, 'unknown') ORDER BY access_datetime DESC))[1] AS last_status,
    COALESCE(EXTRACT(EPOCH FROM (
      MAX(CASE WHEN attendance_status = 'check_out' THEN access_datetime END) -
      MIN(CASE WHEN attendance_status = 'check_in'  THEN access_datetime END)
    )) / 3600.0, 0)::float8 AS gross_hours
  FROM all_records GROUP BY person, department
)
SELECT
  ds.person,
  ds.department,
  ds.event_count,
  ds.first_time,
  ds.last_time,
  ds.last_status,
  COALESCE(bt.hours_break, 0)::float8 AS hours_break,
  TO_CHAR((COALESCE(bt.hours_break, 0) * INTERVAL '1 hour'), 'HH24:MI') AS break_time,
  GREATEST(ds.gross_hours - COALESCE(bt.hours_break, 0), 0)::float8 AS hours_worked,
  TO_CHAR((GREATEST(ds.gross_hours - COALESCE(bt.hours_break, 0), 0) * INTERVAL '1 hour'), 'HH24:MI') AS worked_time
FROM day_summary ds
LEFT JOIN break_totals bt ON ds.person = bt.person
ORDER BY ds.event_count DESC, ds.person";
            var results = await conn.QueryAsync<DayPersonRowResponse>(sql, new { date, dept = department, emp = employee });
            return results;
        }

        public async Task<IEnumerable<string>> GetEmployeesAsync(string? department)
        {
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<string>(
                "SELECT DISTINCT COALESCE(NULLIF(TRIM(person_name), ''), employee_id, 'Unknown') AS name " +
                "FROM access_records " +
                "WHERE (@dept::text IS NULL OR department = @dept) " +
                "  AND person_name IS NOT NULL AND TRIM(person_name) <> '' " +
                "ORDER BY name",
                new { dept = department });
        }
    }
}
