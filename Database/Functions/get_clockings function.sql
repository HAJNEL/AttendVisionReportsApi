-- FUNCTION: public.get_clockings(date, date, text, text, text, uuid)

DROP FUNCTION IF EXISTS public.get_clockings(date, date, text, text, text, uuid);

CREATE OR REPLACE FUNCTION public.get_clockings(
	p_date_from date,
	p_date_to date,
	p_dept text,
	p_employee_id text,
	p_attendance_group_id text,
	p_user_id uuid)
    RETURNS TABLE(date text, person text, employee_id text, department text, access_time text, attendance_status text, authentication_result text)
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    RETURN QUERY
    WITH
    filtered_access_records AS (
      SELECT ar.*
      FROM access_records ar
      LEFT JOIN employees emp ON emp.employee_no = ar.employee_id
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
            p_attendance_group_id IS NULL OR p_attendance_group_id = ''
            OR emp.attendance_group_id = p_attendance_group_id::uuid
        )
    )
    SELECT
        ac.access_date::text AS date,
        ac.clock_person AS person,
        ac.clock_employee_id AS employee_id,
        ac.clock_department AS department,
        ac.clock_access_time AS access_time,
        ac.clock_attendance_status AS attendance_status,
        ac.clock_authentication_result AS authentication_result
    FROM (
        SELECT
            ar.access_date,
            COALESCE(NULLIF(TRIM(ar.person_name), ''), ar.employee_id, 'Unknown')::text AS clock_person,
            COALESCE(ar.employee_id, '')::text AS clock_employee_id,
            COALESCE(ar.department, 'Unknown')::text AS clock_department,
            COALESCE(TO_CHAR(ar.access_time, 'HH24:MI:SS'), '')::text AS clock_access_time,
            COALESCE(ar.attendance_status, '')::text AS clock_attendance_status,
            COALESCE(ar.authentication_result, '')::text AS clock_authentication_result
        FROM filtered_access_records ar
    ) ac
    ORDER BY ac.access_date, ac.clock_access_time, ac.clock_person;
END;
$BODY$;

ALTER FUNCTION public.get_clockings(date, date, text, text, text, uuid)
    OWNER TO postgres;
