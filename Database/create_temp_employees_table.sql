CREATE TABLE IF NOT EXISTS temp_employees (
    id uuid PRIMARY KEY,
    employee_id uuid REFERENCES employees(id) ON DELETE CASCADE,
    has_changes boolean NOT NULL DEFAULT false,
    employee_no text,
    first_name text,
    last_name text,
    department_id uuid REFERENCES departments(id) ON DELETE SET NULL,
    gender int,
    phone_no text,
    email text,
    job_no text,
    remark text,
    photo_base64 text,
    submitted_by uuid REFERENCES users(id) ON DELETE SET NULL,
    created_at timestamp NOT NULL,
    updated_at timestamp NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_temp_employees_employee_id ON temp_employees(employee_id);
CREATE INDEX IF NOT EXISTS ix_temp_employees_department_id ON temp_employees(department_id);
CREATE INDEX IF NOT EXISTS ix_temp_employees_submitted_by ON temp_employees(submitted_by);
