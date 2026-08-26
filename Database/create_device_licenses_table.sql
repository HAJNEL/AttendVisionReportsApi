CREATE TABLE IF NOT EXISTS device_licenses (
    id uuid PRIMARY KEY,
    hik_dev_index_code text NOT NULL,
    company_id uuid REFERENCES companies(id) ON DELETE SET NULL,
    status text NOT NULL DEFAULT 'Active',
    issue_date date NOT NULL,
    expiry_date date NOT NULL,
    notes text
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_device_licenses_hik_dev_index_code ON device_licenses(hik_dev_index_code);
CREATE INDEX IF NOT EXISTS ix_device_licenses_company_id ON device_licenses(company_id);
