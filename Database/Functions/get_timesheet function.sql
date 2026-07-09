-- FUNCTION: public.get_timesheet(date, date, text, text, text, uuid)

-- DROP FUNCTION IF EXISTS public.get_timesheet(date, date, text, text, text, uuid);

CREATE OR REPLACE FUNCTION public.get_timesheet(
	p_date_from date,
	p_date_to date,
	p_dept text,
	p_employee_id text,
	p_employee_type text,
	p_user_id uuid)
    RETURNS TABLE(person text, status text, employee_id text, department text, date text, first_entry text, last_entry text, hours_worked double precision, break_hours double precision, company_code text)
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
      WHERE department_id = (
          SELECT department_id FROM rate
          WHERE (p_employee_type IS NOT NULL AND p_employee_type <> '') AND id = p_employee_type::uuid
      )
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
    all_records AS (
      SELECT
        COALESCE(NULLIF(TRIM(ar.person_name), ''), ar.employee_id, 'Unknown')::text AS person,
        COALESCE(ar.employee_id, '')::text AS employee_id,
        COALESCE(ar.department, 'Unknown')::text AS department,
        ar.access_date,
        ar.access_time,
        ar.access_datetime,
        ar.attendance_status,
        d.id AS department_id,
        d.company_code
      FROM filtered_access_records ar
      LEFT JOIN departments d ON ar.department = d.department_name
    ),
    leave_records AS (
      SELECT
        el.employee_id::text AS employee_id,
        el.full_name::text AS full_name,
        el.department_id,
        el.type,
        el.from_time,
        el.to_time,
        generate_series(el.from_date, el.to_date, INTERVAL '1 day')::date AS access_date
      FROM employee_leave el
      WHERE (el.from_date <= p_date_to AND el.to_date >= p_date_from)
    ),
    combined_days AS (
      SELECT
        arcd.person,
        arcd.employee_id,
        arcd.department,
        arcd.department_id,
        arcd.access_date,
        arcd.company_code
      FROM all_records arcd
      GROUP BY arcd.person, arcd.employee_id, arcd.department, arcd.department_id, arcd.access_date, arcd.company_code
      UNION
      SELECT
        lr.full_name::text AS person,
        lr.employee_id::text,
        d.department_name::text AS department,
        lr.department_id,
        lr.access_date,
        d.company_code
      FROM leave_records lr
      JOIN departments d ON lr.department_id = d.id
      WHERE (p_dept IS NULL OR d.department_name = p_dept)
        AND (p_employee_id IS NULL OR lr.employee_id = p_employee_id)
    ),
    daily_bounds AS (
      SELECT
        cd.person,
        cd.employee_id,
        cd.department,
        cd.department_id,
        cd.access_date,
        cd.company_code,
        MIN(CASE WHEN ar.attendance_status = 'check_in' THEN ar.access_datetime END) AS raw_in_dt,
        MAX(CASE WHEN ar.attendance_status = 'check_out' THEN ar.access_datetime END) AS raw_out_dt
      FROM combined_days cd
      LEFT JOIN all_records ar ON cd.person = ar.person AND cd.access_date = ar.access_date
      GROUP BY cd.person, cd.employee_id, cd.department, cd.department_id, cd.access_date, cd.company_code
    ),
    time_override AS (
      SELECT
        t.id AS override_id,
        t.department_id,
        t.from_time,
        t.to_time,
        t.override_time
      FROM time_overrides t
    ),
    effective_bounds AS (
      SELECT
        db.*,
        CASE
          WHEN db.raw_in_dt IS NOT NULL AND tover.override_time IS NOT NULL THEN (db.access_date + tover.override_time)
          ELSE db.raw_in_dt
        END AS eff_in_dt
      FROM daily_bounds db
      LEFT JOIN LATERAL (
        SELECT t.override_time
        FROM time_override t
        WHERE t.department_id = db.department_id
          AND db.raw_in_dt::time >= t.from_time AND db.raw_in_dt::time < t.to_time
        ORDER BY t.from_time DESC
        LIMIT 1
      ) tover ON TRUE
    ),
    deduped AS (
      SELECT
        ar.*,
        LAG(ar.attendance_status) OVER (PARTITION BY ar.person, ar.access_date ORDER BY ar.access_datetime) AS prev_status
      FROM all_records ar
    ),
    first_breaks AS (
      SELECT
        d.person,
        d.employee_id,
        d.department,
        d.access_date,
        d.access_datetime
      FROM deduped d
      WHERE d.attendance_status = 'break_out'
        AND (d.prev_status IS NULL OR d.prev_status != 'break_out')
    ),
    break_durations AS (
      SELECT
        fb.person,
        fb.employee_id,
        fb.department,
        fb.access_date,
        fb.access_datetime AS raw_break_start,
        (
          SELECT MIN(ar.access_datetime)
          FROM all_records ar
          WHERE ar.person = fb.person
            AND ar.access_date = fb.access_date
            AND ar.attendance_status = 'break_in'
            AND ar.access_datetime > fb.access_datetime
        ) AS raw_break_end,
        eb.eff_in_dt
      FROM first_breaks fb
      JOIN effective_bounds eb
        ON fb.person = eb.person AND fb.access_date = eb.access_date
    ),
    break_totals AS (
      SELECT
        bd.person,
        bd.employee_id,
        bd.department,
        bd.access_date,
        COALESCE(
          SUM(
            CASE
              WHEN bd.raw_break_end IS NOT NULL
                THEN EXTRACT(EPOCH FROM (bd.raw_break_end - GREATEST(bd.raw_break_start, bd.eff_in_dt))) / 3600.0
              ELSE 0
            END
          ) FILTER (WHERE bd.raw_break_end > bd.eff_in_dt), 0
        )::float8 AS break_hours
      FROM break_durations bd
      GROUP BY bd.person, bd.employee_id, bd.department, bd.access_date
    ),
    leave_days AS (
      SELECT
        lr.employee_id::text AS employee_id,
        lr.full_name::text AS full_name,
        lr.access_date,
        MAX(lr.type) AS type,
        MIN(lr.from_time) AS from_time,
        MAX(lr.to_time) AS to_time
      FROM leave_records lr
      GROUP BY lr.employee_id, lr.full_name, lr.access_date
    )
    SELECT
      eb.person,
      CASE
        WHEN (
          COALESCE(EXTRACT(EPOCH FROM (GREATEST(eb.raw_out_dt, eb.eff_in_dt) - eb.eff_in_dt)) / 3600.0, 0)
          - COALESCE(bt.break_hours, 0)
        ) > 0.0166 THEN 'Worked'
        WHEN ld.type IS NOT NULL THEN INITCAP(CONCAT(INITCAP(ld.type), ' Leave'))
        ELSE 'Issue'
      END AS status,
      eb.employee_id,
      eb.department,
      eb.access_date::text AS date,
      COALESCE((eb.eff_in_dt::time)::text, TO_CHAR(ld.from_time, 'HH24:MI:SS'), '') AS first_entry,
      COALESCE((eb.raw_out_dt::time)::text, TO_CHAR(ld.to_time, 'HH24:MI:SS'), '') AS last_entry,
      -- Leave days have no clock events, so fall back to the leave record's own
      -- from_time/to_time span for Total Span; break stays 0 (nothing to deduct).
      CASE
        WHEN eb.eff_in_dt IS NOT NULL THEN
          COALESCE(EXTRACT(EPOCH FROM (GREATEST(eb.raw_out_dt, eb.eff_in_dt) - eb.eff_in_dt)) / 3600.0, 0)
        WHEN ld.type IS NOT NULL AND ld.from_time IS NOT NULL AND ld.to_time IS NOT NULL THEN
          GREATEST(EXTRACT(EPOCH FROM (ld.to_time - ld.from_time)) / 3600.0, 0)
        ELSE 0
      END::float8 AS hours_worked,
      CASE
        WHEN eb.eff_in_dt IS NOT NULL THEN COALESCE(bt.break_hours, 0)
        ELSE 0
      END::float8 AS break_hours,
      eb.company_code
    FROM effective_bounds eb
    LEFT JOIN break_totals bt
      ON eb.person = bt.person AND eb.access_date = bt.access_date
    LEFT JOIN leave_days ld
      ON (ld.employee_id = eb.employee_id OR ld.full_name = eb.person)
        AND ld.access_date = eb.access_date
    ORDER BY eb.access_date, eb.person;
END;
$BODY$;

ALTER FUNCTION public.get_timesheet(date, date, text, text, text, uuid)
    OWNER TO postgres;
