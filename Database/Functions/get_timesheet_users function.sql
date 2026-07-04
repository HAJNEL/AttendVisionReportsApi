-- FUNCTION: public.get_timesheet_users(date, date, text, text, uuid)
-- Not currently referenced by the API/webapp code; kept for reference.

-- DROP FUNCTION IF EXISTS public.get_timesheet_users(date, date, text, text, uuid);

CREATE OR REPLACE FUNCTION public.get_timesheet_users(
	p_date_from date,
	p_date_to date,
	p_dept text,
	p_employee_id text,
	p_user_id uuid)
    RETURNS TABLE(name text)
    LANGUAGE 'plpgsql'
AS $BODY$
BEGIN
    RETURN QUERY
    SELECT DISTINCT COALESCE(NULLIF(TRIM(ar.person_name), ''), ar.employee_id)::text AS name
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
      AND COALESCE(NULLIF(TRIM(ar.person_name), ''), ar.employee_id) IS NOT NULL
    ORDER BY name;
END;
$BODY$;

ALTER FUNCTION public.get_timesheet_users(date, date, text, text, uuid)
    OWNER TO postgres;
