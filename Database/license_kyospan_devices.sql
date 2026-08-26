-- One-off data fix: issues a 1-year "Active" door license for the 8 Kyospan
-- devices currently showing "Not Licensed" on the Admin Control Center.
-- (Kyospan - Protea Heights, code 54, already has an Active license and is
-- intentionally excluded.)
--
-- Devices come live from HikCentral - there's no local device table, so a
-- license only exists if a row is present in device_licenses keyed by
-- hik_dev_index_code. issue_date is set to the earliest access_records.date
-- for that device's department (matched by name); expiry_date is issue_date
-- + 1 year, matching the existing Protea Heights license's term.

-- 1. Preview: review the matched department and computed issue date for
--    each device before committing. Any NULL first_access_date means no
--    access_records exist for that department name - fix the name mapping
--    below (or that department's data) before running the insert.
SELECT
    p.hik_dev_index_code,
    p.department_name,
    ar.first_access_date,
    (ar.first_access_date + INTERVAL '1 year')::date AS expiry_date
FROM (
    VALUES
        ('51', 'Okavango Crossing'),
        ('50', 'Mitchells Plain'),
        ('49', 'Hermanus Station'),
        ('48', 'Whale Coast Mall'),
        ('25', 'Eikestad'),
        ('23', 'Somerset West Hyper'),
        ('14', 'Riverland'),
        ('13', 'Century City')
) AS p(hik_dev_index_code, department_name)
LEFT JOIN LATERAL (
    SELECT MIN(access_date) AS first_access_date
    FROM access_records
    WHERE department = p.department_name
) ar ON true
ORDER BY p.hik_dev_index_code;

-- 2. Apply: insert (or repair) the license row for each device.
BEGIN;

INSERT INTO device_licenses (id, hik_dev_index_code, company_id, status, issue_date, expiry_date, notes)
SELECT
    gen_random_uuid(),
    p.hik_dev_index_code,
    (SELECT id FROM companies WHERE name = 'Kyospan' LIMIT 1),
    'Active',
    ar.first_access_date,
    (ar.first_access_date + INTERVAL '1 year')::date,
    NULL
FROM (
    VALUES
        ('51', 'Okavango Crossing'),
        ('50', 'Mitchells Plain'),
        ('49', 'Hermanus Station'),
        ('48', 'Whale Coast Mall'),
        ('25', 'Eikestad'),
        ('23', 'Somerset West Hyper'),
        ('14', 'Riverland'),
        ('13', 'Century City')
) AS p(hik_dev_index_code, department_name)
JOIN LATERAL (
    SELECT MIN(access_date) AS first_access_date
    FROM access_records
    WHERE department = p.department_name
) ar ON true
WHERE ar.first_access_date IS NOT NULL
ON CONFLICT (hik_dev_index_code) DO UPDATE SET
    status = EXCLUDED.status,
    issue_date = EXCLUDED.issue_date,
    expiry_date = EXCLUDED.expiry_date,
    company_id = EXCLUDED.company_id;

COMMIT;

-- 3. Confirm: should show 8 Active rows, one per device above.
SELECT hik_dev_index_code, status, issue_date, expiry_date
FROM device_licenses
WHERE hik_dev_index_code IN ('51', '50', '49', '48', '25', '23', '14', '13')
ORDER BY hik_dev_index_code;
