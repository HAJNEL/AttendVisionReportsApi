using System.Globalization;
using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Services
{
    public class ReportsService(NpgsqlDataSource dataSource) : IReportsService
    {

        public async Task<IEnumerable<dynamic>> GetIssuesAsync(string dateFrom, string dateTo, string? department)
        {
            var df = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dt = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(@"
WITH base AS (
  SELECT COALESCE(NULLIF(TRIM(person_name), ''), employee_id, 'Unknown') AS person,
    COALESCE(employee_id, '')::text AS employee_id,
    COALESCE(department, 'Unknown') AS department,
    access_datetime, access_date, attendance_status,
    COALESCE(failed, false) AS is_failed
  FROM access_records
  WHERE access_date BETWEEN @dateFrom::date AND @dateTo::date
    AND (@dept::text IS NULL OR department = @dept)
),
failed_issues AS (
  SELECT access_date::text AS date, TO_CHAR(access_datetime, 'HH24:MI')::text AS time_of,
    person, employee_id, department, 'failed_attempt'::text AS issue_type
  FROM base WHERE is_failed = true
),
normal_records AS (SELECT * FROM base WHERE is_failed = false),
no_checkout AS (
  SELECT access_date::text AS date, MIN(TO_CHAR(access_datetime, 'HH24:MI'))::text AS time_of,
    person, employee_id, department, 'no_checkout'::text AS issue_type
  FROM normal_records GROUP BY person, employee_id, department, access_date
  HAVING MAX(CASE WHEN attendance_status = 'check_in'  THEN 1 ELSE 0 END) = 1
     AND MAX(CASE WHEN attendance_status = 'check_out' THEN 1 ELSE 0 END) = 0
),
break_events AS (
  SELECT person, employee_id, department, access_date, access_datetime, attendance_status,
    LEAD(attendance_status) OVER (PARTITION BY person, access_date ORDER BY access_datetime) AS next_status
  FROM normal_records WHERE attendance_status IN ('break_out', 'break_in')
),
unmatched_break AS (
  SELECT access_date::text AS date, TO_CHAR(access_datetime, 'HH24:MI')::text AS time_of,
    person, employee_id, department, 'unmatched_break'::text AS issue_type
  FROM break_events WHERE attendance_status = 'break_out' AND (next_status IS NULL OR next_status != 'break_in')
)
SELECT date, time_of, person, employee_id, department, issue_type
FROM (SELECT * FROM failed_issues UNION ALL SELECT * FROM no_checkout UNION ALL SELECT * FROM unmatched_break) combined
ORDER BY date, time_of NULLS LAST, issue_type, person",
                new { dateFrom = df.ToString("yyyy-MM-dd"), dateTo = dt.ToString("yyyy-MM-dd"), dept = department });
        }

        public async Task<IEnumerable<dynamic>> GetClockingsAsync(string dateFrom, string dateTo, string? dept, string? user)
        {
            var df = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dt = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(
                "SELECT access_date::text AS date," +
                "  COALESCE(NULLIF(TRIM(person_name), ''), employee_id, 'Unknown') AS person," +
                "  COALESCE(employee_id, '')::text AS employee_id," +
                "  COALESCE(department, 'Unknown') AS department," +
                "  COALESCE(TO_CHAR(access_time, 'HH24:MI:SS'), '')::text AS access_time," +
                "  COALESCE(attendance_status, '')::text AS attendance_status," +
                "  COALESCE(authentication_result, '')::text AS authentication_result " +
                "FROM access_records " +
                "WHERE access_date BETWEEN @dateFrom::date AND @dateTo::date " +
                "  AND (@dept::text IS NULL OR department = @dept) " +
                "  AND (@user::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @user) " +
                "ORDER BY access_date, access_time, person",
                new { dateFrom = df.ToString("yyyy-MM-dd"), dateTo = dt.ToString("yyyy-MM-dd"), dept, user });
        }

        public async Task<IEnumerable<string>> GetTimesheetUsersAsync(string dateFrom, string dateTo, string? dept)
        {
            var df = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dt = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<string>(
                "SELECT DISTINCT COALESCE(NULLIF(TRIM(person_name), ''), employee_id) AS name " +
                "FROM access_records " +
                "WHERE access_date BETWEEN @dateFrom::date AND @dateTo::date " +
                "  AND (@dept::text IS NULL OR department = @dept) " +
                "  AND COALESCE(NULLIF(TRIM(person_name), ''), employee_id) IS NOT NULL " +
                "ORDER BY name",
                new { dateFrom = df.ToString("yyyy-MM-dd"), dateTo = dt.ToString("yyyy-MM-dd"), dept });
        }

        public async Task<IEnumerable<dynamic>> GetTimesheetAsync(string dateFrom, string dateTo, string? dept, string? user)
        {
            var df = DateOnly.ParseExact(dateFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dt = DateOnly.ParseExact(dateTo, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(@"
WITH all_records AS (
  SELECT COALESCE(NULLIF(TRIM(ar.person_name), ''), ar.employee_id, 'Unknown') AS person,
    COALESCE(ar.employee_id, '') AS employee_id,
    COALESCE(ar.department, 'Unknown') AS department,
    ar.access_date, ar.access_time, ar.access_datetime, ar.attendance_status,
    d.id AS department_id
  FROM access_records ar
  LEFT JOIN departments d ON ar.department = d.department_name
  WHERE ar.access_date BETWEEN @dateFrom::date AND @dateTo::date
    AND (@dept::text IS NULL OR ar.department = @dept)
    AND (@user::text IS NULL OR COALESCE(NULLIF(TRIM(ar.person_name), ''), ar.employee_id) = @user)
),
daily_bounds AS (
  SELECT person, employee_id, department, department_id, access_date,
    MIN(CASE WHEN attendance_status = 'check_in' THEN access_datetime END) AS raw_in_dt,
    MAX(CASE WHEN attendance_status = 'check_out' THEN access_datetime END) AS raw_out_dt
  FROM all_records GROUP BY person, employee_id, department, department_id, access_date
),
time_override AS (
  SELECT t.id AS override_id, t.department_id, t.from_time, t.to_time, t.override_time
  FROM time_overrides t
),
effective_bounds AS (
  SELECT db.*, 
    CASE 
      WHEN db.raw_in_dt IS NOT NULL AND tover.override_time IS NOT NULL THEN (db.access_date + tover.override_time)
      ELSE db.raw_in_dt 
    END AS eff_in_dt
  FROM daily_bounds db
  LEFT JOIN LATERAL (
    SELECT override_time FROM time_override t
    WHERE t.department_id = db.department_id
      AND db.raw_in_dt::time >= t.from_time AND db.raw_in_dt::time < t.to_time
    ORDER BY t.from_time DESC LIMIT 1
  ) tover ON TRUE
),
deduped AS (
  SELECT ar.*, LAG(ar.attendance_status) OVER (PARTITION BY ar.person, ar.access_date ORDER BY ar.access_datetime) AS prev_status
  FROM all_records ar
),
first_breaks AS (
  SELECT d.person, d.employee_id, d.department, d.access_date, d.access_datetime 
  FROM deduped d
  WHERE d.attendance_status = 'break_out' AND (d.prev_status IS NULL OR d.prev_status != 'break_out')
),
break_durations AS (
  SELECT fb.person, fb.employee_id, fb.department, fb.access_date, 
    fb.access_datetime AS raw_break_start,
    (SELECT MIN(ar.access_datetime) FROM all_records ar
     WHERE ar.person = fb.person AND ar.access_date = fb.access_date
       AND ar.attendance_status = 'break_in' AND ar.access_datetime > fb.access_datetime) AS raw_break_end,
    eb.eff_in_dt
  FROM first_breaks fb
  JOIN effective_bounds eb ON fb.person = eb.person AND fb.access_date = eb.access_date
),
break_totals AS (
  SELECT person, employee_id, department, access_date,
    COALESCE(SUM(CASE 
      WHEN raw_break_end IS NOT NULL THEN 
        EXTRACT(EPOCH FROM (raw_break_end - GREATEST(raw_break_start, eff_in_dt))) / 3600.0 
      ELSE 0 
    END) FILTER (WHERE raw_break_end > eff_in_dt), 0)::float8 AS break_hours
  FROM break_durations 
  GROUP BY person, employee_id, department, access_date
)
SELECT 
  eb.person, eb.employee_id, eb.department,
  eb.access_date::text AS date,
  COALESCE((eb.eff_in_dt::time)::text, '') AS first_entry,
  COALESCE((eb.raw_out_dt::time)::text, '') AS last_entry,
  COALESCE(EXTRACT(EPOCH FROM (GREATEST(eb.raw_out_dt, eb.eff_in_dt) - eb.eff_in_dt)) / 3600.0, 0)::float8 AS hours_worked,
  COALESCE(bt.break_hours, 0)::float8 AS break_hours
FROM effective_bounds eb
LEFT JOIN break_totals bt ON eb.person = bt.person AND eb.access_date = bt.access_date
ORDER BY eb.access_date, eb.person",
                new { dateFrom = df.ToString("yyyy-MM-dd"), dateTo = dt.ToString("yyyy-MM-dd"), dept, user });
        }
    }
}
