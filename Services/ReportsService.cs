using Dapper;
using Npgsql;

namespace AttendVisionReportsApi.Services
{
    public class ReportsService(NpgsqlDataSource dataSource) : IReportsService
    {

        public async Task<IEnumerable<dynamic>> GetIssuesAsync(string dateFrom, string dateTo, string? department)
        {
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
                new { dateFrom, dateTo, dept = department });
        }

        public async Task<IEnumerable<dynamic>> GetClockingsAsync(string dateFrom, string dateTo, string? dept, string? user)
        {
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
                new { dateFrom, dateTo, dept, user });
        }

        public async Task<IEnumerable<string>> GetTimesheetUsersAsync(string dateFrom, string dateTo, string? dept)
        {
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<string>(
                "SELECT DISTINCT COALESCE(NULLIF(TRIM(person_name), ''), employee_id) AS name " +
                "FROM access_records " +
                "WHERE access_date BETWEEN @dateFrom::date AND @dateTo::date " +
                "  AND (@dept::text IS NULL OR department = @dept) " +
                "  AND COALESCE(NULLIF(TRIM(person_name), ''), employee_id) IS NOT NULL " +
                "ORDER BY name",
                new { dateFrom, dateTo, dept });
        }

        public async Task<IEnumerable<dynamic>> GetTimesheetAsync(string dateFrom, string dateTo, string? dept, string? user)
        {
            using var conn = dataSource.CreateConnection();
            return await conn.QueryAsync<dynamic>(@"
WITH all_records AS (
  SELECT COALESCE(NULLIF(TRIM(person_name), ''), employee_id, 'Unknown') AS person,
    COALESCE(employee_id, '') AS employee_id,
    COALESCE(department, 'Unknown') AS department,
    access_date, access_time, access_datetime, attendance_status
  FROM access_records
  WHERE access_date BETWEEN @dateFrom::date AND @dateTo::date
    AND (@dept::text IS NULL OR department = @dept)
    AND (@user::text IS NULL OR COALESCE(NULLIF(TRIM(person_name), ''), employee_id) = @user)
),
deduped AS (
  SELECT *, LAG(attendance_status) OVER (PARTITION BY person, access_date ORDER BY access_datetime) AS prev_status
  FROM all_records
),
first_breaks AS (
  SELECT person, employee_id, department, access_date, access_datetime FROM deduped
  WHERE attendance_status = 'break_out' AND (prev_status IS NULL OR prev_status != 'break_out')
),
break_durations AS (
  SELECT fb.person, fb.employee_id, fb.department, fb.access_date, fb.access_datetime AS break_start,
    (SELECT MIN(ar.access_datetime) FROM all_records ar
     WHERE ar.person = fb.person AND ar.access_date = fb.access_date
       AND ar.attendance_status = 'break_in' AND ar.access_datetime > fb.access_datetime) AS break_end
  FROM first_breaks fb
),
break_totals AS (
  SELECT person, employee_id, department, access_date,
    COALESCE(SUM(CASE WHEN break_end IS NOT NULL THEN EXTRACT(EPOCH FROM (break_end - break_start)) / 3600.0 ELSE 0 END), 0)::float8 AS break_hours
  FROM break_durations GROUP BY person, employee_id, department, access_date
),
day_summary AS (
  SELECT person, employee_id, department, access_date,
    COALESCE(MIN(CASE WHEN attendance_status = 'check_in'  THEN access_time END)::text, '') AS first_entry,
    COALESCE(MAX(CASE WHEN attendance_status = 'check_out' THEN access_time END)::text, '') AS last_entry,
    COALESCE(EXTRACT(EPOCH FROM (
      MAX(CASE WHEN attendance_status = 'check_out' THEN access_datetime END) -
      MIN(CASE WHEN attendance_status = 'check_in'  THEN access_datetime END)
    )) / 3600.0, 0)::float8 AS hours_worked
  FROM all_records GROUP BY person, employee_id, department, access_date
)
SELECT ds.person, ds.employee_id, ds.department,
  ds.access_date::text AS date, ds.first_entry, ds.last_entry, ds.hours_worked,
  COALESCE(bt.break_hours, 0)::float8 AS break_hours
FROM day_summary ds
LEFT JOIN break_totals bt ON ds.person = bt.person AND ds.access_date = bt.access_date
ORDER BY ds.access_date, ds.person",
                new { dateFrom, dateTo, dept, user });
        }
    }
}
