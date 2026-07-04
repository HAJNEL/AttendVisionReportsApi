-- FUNCTION: public.get_attendance_issues(date, date, text, text, text, uuid)

-- DROP FUNCTION IF EXISTS public.get_attendance_issues(date, date, text, text, text, uuid);

CREATE OR REPLACE FUNCTION public.get_attendance_issues(
	p_date_from date,
	p_date_to date,
	p_dept text,
	p_employee_id text,
	p_employee_type text,
	p_user_id uuid)
    RETURNS TABLE(date text, time_of text, person text, employee_id text, department text, issue_type text)
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
  RETURN QUERY
    WITH
    rate AS (
      SELECT * FROM department_payment_rates
      WHERE (p_employee_type IS NOT NULL AND p_employee_type <> '')
            AND id = p_employee_type::uuid
    ),
    rate_match_key AS (
      SELECT match_key, department_id FROM rate
    ),
    department_keys AS (
      SELECT match_key FROM department_payment_rates
      WHERE department_id = (SELECT department_id FROM rate WHERE (p_employee_type IS NOT NULL AND p_employee_type <> '') AND id = p_employee_type::uuid)
        AND match_key IS NOT NULL AND match_key <> ''
    ),
    filtered_access_records AS (
      SELECT ar.*
      FROM access_records ar
      LEFT JOIN rate_match_key rmk ON 1=1
      WHERE
        ar.access_date BETWEEN p_date_from AND p_date_to
        AND (
          (p_dept IS NULL AND
            EXISTS (
                SELECT 1 FROM department_users du
                JOIN departments d ON du.department_id = d.id
                WHERE du.user_id = p_user_id AND d.department_name = ar.department
            )
          )
          OR (p_dept IS NOT NULL AND ar.department = p_dept)
        )
        AND (p_employee_id IS NULL OR ar.employee_id = p_employee_id)
        AND (
          p_employee_type IS NULL OR p_employee_type = ''
          OR (
            (SELECT match_key FROM rate_match_key LIMIT 1) IS NOT NULL
            AND POSITION((SELECT match_key FROM rate_match_key LIMIT 1) IN ar.employee_id) > 0
          )
          OR (
            (SELECT match_key FROM rate_match_key LIMIT 1) IS NULL
            AND NOT EXISTS (
              SELECT 1 FROM department_keys dk
              WHERE POSITION(dk.match_key IN ar.employee_id) > 0
            )
          )
        )
    ),
    base AS (
      SELECT
        COALESCE(NULLIF(TRIM(ar.person_name), ''), ar.employee_id, 'Unknown')::text AS i_person,
        COALESCE(ar.employee_id, '')::text AS i_employee_id,
        COALESCE(ar.department, 'Unknown')::text AS i_department,
        ar.access_datetime AS i_access_datetime,
        ar.access_date AS i_access_date,
        ar.attendance_status AS i_attendance_status,
        COALESCE(ar.failed, false) AS i_is_failed
      FROM filtered_access_records ar
    ),
    failed_issues AS (
      SELECT
        base.i_access_date::text AS i_date,
        TO_CHAR(base.i_access_datetime, 'HH24:MI')::text AS i_time_of,
        base.i_person,
        base.i_employee_id,
        base.i_department,
        'failed_attempt'::text AS i_issue_type
      FROM base
      WHERE base.i_is_failed = true
    ),
    normal_records AS (
      SELECT * FROM base WHERE i_is_failed = false
    ),
    no_checkout AS (
      SELECT
        nr.i_access_date::text AS i_date,
        MIN(TO_CHAR(nr.i_access_datetime, 'HH24:MI'))::text AS i_time_of,
        nr.i_person,
        nr.i_employee_id,
        nr.i_department,
        'no_checkout'::text AS i_issue_type
      FROM normal_records nr
      GROUP BY nr.i_person, nr.i_employee_id, nr.i_department, nr.i_access_date
      HAVING MAX(CASE WHEN nr.i_attendance_status = 'check_in' THEN 1 ELSE 0 END) = 1
         AND MAX(CASE WHEN nr.i_attendance_status = 'check_out' THEN 1 ELSE 0 END) = 0
    ),
    break_events AS (
      SELECT
        nr.i_person,
        nr.i_employee_id,
        nr.i_department,
        nr.i_access_date,
        nr.i_access_datetime,
        nr.i_attendance_status,
        LEAD(nr.i_attendance_status) OVER (
            PARTITION BY nr.i_person, nr.i_access_date
            ORDER BY nr.i_access_datetime
        ) AS i_next_status
      FROM normal_records nr
      WHERE nr.i_attendance_status IN ('break_out', 'break_in')
    ),
    unmatched_break AS (
      SELECT
        be.i_access_date::text AS i_date,
        TO_CHAR(be.i_access_datetime, 'HH24:MI')::text AS i_time_of,
        be.i_person,
        be.i_employee_id,
        be.i_department,
        'unmatched_break'::text AS i_issue_type
      FROM break_events be
      WHERE be.i_attendance_status = 'break_out'
        AND (be.i_next_status IS NULL OR be.i_next_status != 'break_in')
    )
    SELECT
      combined.i_date AS date,
      combined.i_time_of AS time_of,
      combined.i_person AS person,
      combined.i_employee_id AS employee_id,
      combined.i_department AS department,
      combined.i_issue_type AS issue_type
    FROM (
      SELECT i_date, i_time_of, i_person, i_employee_id, i_department, i_issue_type FROM failed_issues
      UNION ALL
      SELECT i_date, i_time_of, i_person, i_employee_id, i_department, i_issue_type FROM no_checkout
      UNION ALL
      SELECT i_date, i_time_of, i_person, i_employee_id, i_department, i_issue_type FROM unmatched_break
    ) AS combined
    ORDER BY combined.i_date, combined.i_time_of NULLS LAST, combined.i_issue_type, combined.i_person;
END;
$BODY$;

ALTER FUNCTION public.get_attendance_issues(date, date, text, text, text, uuid)
    OWNER TO postgres;

-- FUNCTION: public.get_attendance_issues(date, date, text, text, uuid)
-- Legacy overload without p_employee_type; not called by the API, kept for reference.

-- DROP FUNCTION IF EXISTS public.get_attendance_issues(date, date, text, text, uuid);

CREATE OR REPLACE FUNCTION public.get_attendance_issues(
	p_date_from date,
	p_date_to date,
	p_dept text,
	p_employee_id text,
	p_user_id uuid)
    RETURNS TABLE(date text, time_of text, person text, employee_id text, department text, issue_type text)
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
  RETURN QUERY
    WITH base AS (
      SELECT
        COALESCE(NULLIF(TRIM(ar.person_name), ''), ar.employee_id, 'Unknown')::text AS i_person,
        COALESCE(ar.employee_id, '')::text AS i_employee_id,
        COALESCE(ar.department, 'Unknown')::text AS i_department,
        ar.access_datetime AS i_access_datetime,
        ar.access_date AS i_access_date,
        ar.attendance_status AS i_attendance_status,
        COALESCE(ar.failed, false) AS i_is_failed
      FROM access_records ar
      WHERE ar.access_date BETWEEN p_date_from AND p_date_to
        AND (
          (p_dept IS NULL AND
            EXISTS (
                SELECT 1 FROM department_users du
                JOIN departments d ON du.department_id = d.id
                WHERE du.user_id = p_user_id AND d.department_name = ar.department
            )
          )
          OR (p_dept IS NOT NULL AND ar.department = p_dept)
        )
        AND (p_employee_id IS NULL OR ar.employee_id = p_employee_id)
    ),
    failed_issues AS (
      SELECT
        base.i_access_date::text AS i_date,
        TO_CHAR(base.i_access_datetime, 'HH24:MI')::text AS i_time_of,
        base.i_person,
        base.i_employee_id,
        base.i_department,
        'failed_attempt'::text AS i_issue_type
      FROM base
      WHERE base.i_is_failed = true
    ),
    normal_records AS (
      SELECT * FROM base WHERE i_is_failed = false
    ),
    no_checkout AS (
      SELECT
        nr.i_access_date::text AS i_date,
        MIN(TO_CHAR(nr.i_access_datetime, 'HH24:MI'))::text AS i_time_of,
        nr.i_person,
        nr.i_employee_id,
        nr.i_department,
        'no_checkout'::text AS i_issue_type
      FROM normal_records nr
      GROUP BY nr.i_person, nr.i_employee_id, nr.i_department, nr.i_access_date
      HAVING MAX(CASE WHEN nr.i_attendance_status = 'check_in' THEN 1 ELSE 0 END) = 1
         AND MAX(CASE WHEN nr.i_attendance_status = 'check_out' THEN 1 ELSE 0 END) = 0
    ),
    break_events AS (
      SELECT
        nr.i_person,
        nr.i_employee_id,
        nr.i_department,
        nr.i_access_date,
        nr.i_access_datetime,
        nr.i_attendance_status,
        LEAD(nr.i_attendance_status) OVER (
            PARTITION BY nr.i_person, nr.i_access_date
            ORDER BY nr.i_access_datetime
        ) AS i_next_status
      FROM normal_records nr
      WHERE nr.i_attendance_status IN ('break_out', 'break_in')
    ),
    unmatched_break AS (
      SELECT
        be.i_access_date::text AS i_date,
        TO_CHAR(be.i_access_datetime, 'HH24:MI')::text AS i_time_of,
        be.i_person,
        be.i_employee_id,
        be.i_department,
        'unmatched_break'::text AS i_issue_type
      FROM break_events be
      WHERE be.i_attendance_status = 'break_out'
        AND (be.i_next_status IS NULL OR be.i_next_status != 'break_in')
    )
    SELECT
      combined.i_date AS date,
      combined.i_time_of AS time_of,
      combined.i_person AS person,
      combined.i_employee_id AS employee_id,
      combined.i_department AS department,
      combined.i_issue_type AS issue_type
    FROM (
      SELECT i_date, i_time_of, i_person, i_employee_id, i_department, i_issue_type FROM failed_issues
      UNION ALL
      SELECT i_date, i_time_of, i_person, i_employee_id, i_department, i_issue_type FROM no_checkout
      UNION ALL
      SELECT i_date, i_time_of, i_person, i_employee_id, i_department, i_issue_type FROM unmatched_break
    ) AS combined
    ORDER BY combined.i_date, combined.i_time_of NULLS LAST, combined.i_issue_type, combined.i_person;
END;
$BODY$;

ALTER FUNCTION public.get_attendance_issues(date, date, text, text, uuid)
    OWNER TO postgres;
