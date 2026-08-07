CREATE TABLE IF NOT EXISTS employees (
    id uuid PRIMARY KEY,
    hikcentral_person_id text NOT NULL,
    employee_no text,
    full_name text,
    first_name text,
    last_name text,
    gender int,
    department_id uuid REFERENCES departments(id) ON DELETE SET NULL,
    org_index_code text,
    position text,
    phone_no text,
    email text,
    job_no text,
    remark text,
    begin_time timestamp,
    end_time timestamp,
    photo_base64 text,
    current_shift_name text,
    current_shift_on_duty text,
    current_shift_off_duty text,
    last_synced_at timestamp
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_employees_hikcentral_person_id ON employees(hikcentral_person_id);
CREATE INDEX IF NOT EXISTS ix_employees_employee_no ON employees(employee_no);
CREATE INDEX IF NOT EXISTS ix_employees_department_id ON employees(department_id);

CREATE TABLE IF NOT EXISTS employee_access_levels (
    id uuid PRIMARY KEY,
    employee_id uuid NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    template_id text,
    template_name text
);

CREATE INDEX IF NOT EXISTS ix_employee_access_levels_employee_id ON employee_access_levels(employee_id);
