ALTER TABLE employees ADD COLUMN IF NOT EXISTS attendance_group_id uuid REFERENCES attendance_groups(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS ix_employees_attendance_group_id ON employees(attendance_group_id);
