-- Assigns employees to their attendance group, matched by employee_no
-- (the stable HikCentral personCode), sourced from the HikCentral
-- "Person Information" export - the Position column there holds
-- Casual/Permanent/Weekend for each person.

-- 1. Verify: should return zero rows. Any row here is an employee_no
--    with a typo, or an employee not yet synced into this database.
WITH expected(employee_no) AS (
    VALUES
    ('RIVERCAS10'), ('CENCCAS10'), ('CENCCAS9'), ('CENCCAS8'), ('MITCHCAS1'), ('CENCCAS7'), ('HERCAS1'), ('WCMCAS1'),
    ('WCMCAS2'), ('RIVERCAS9'), ('WEIK3'), ('WEIK2'), ('WEIK1'), ('SWHCAS2'), ('SWHCAS1'), ('RIVERCAS8'),
    ('RIVERCAS7'), ('RIVERCAS6'), ('RIVERCAS5'), ('RIVERCAS4'), ('RIVERCAS3'), ('CENCCAS6'), ('CENCCAS5'), ('CENCCAS4'),
    ('CENCCAS3'), ('CENCCAS2'), ('RIVERCAS2'), ('WECENC6'), ('WECENC5'), ('WECENC4'), ('WECENC3'), ('WECENC2'),
    ('WECENC1'), ('CENCCAS1'), ('WERIV3'), ('RIVERCAS1'), ('CVND5'), ('CVND3'), ('MITCH1'), ('MITCH2'),
    ('MITCH11'), ('MITCH10'), ('MITCH13'), ('MITCH6'), ('MITCH12'), ('CHER5'), ('CHER4'), ('CHER1'),
    ('CHER3'), ('CHER6'), ('CHER2'), ('CHER8'), ('CHER7'), ('WCM5'), ('WCM6'), ('WCM4'),
    ('WCM9'), ('WCM2'), ('WCM8'), ('WCM3'), ('WCM7'), ('WCM1'), ('EIK5'), ('EIK2'),
    ('EIK3'), ('EIK1'), ('EIK4'), ('EIK6'), ('EIK7'), ('WESWH2'), ('WESWH6'), ('WESWH1'),
    ('SWH12'), ('WESWH7'), ('WESWH5'), ('WESWH3'), ('SWH4'), ('SWH11'), ('SWH5'), ('WESWH4'),
    ('SWH2'), ('SWH9'), ('CENC12'), ('CENC11'), ('CENC10'), ('CENC9'), ('CENC8'), ('CENC7'),
    ('CENC5'), ('CENC4'), ('CENC3'), ('CENC1'), ('WERIV1'), ('WERIV2'), ('WERIV4'), ('RIVER9'),
    ('RIVER7'), ('RIVER6'), ('RIVER5'), ('RIVER4'), ('RIVER3'), ('RIVER2'), ('RIVER1'), ('CVND7'),
    ('CVND6'), ('CVND4'), ('CVND2'), ('CVND1'), ('WEMITCH3'), ('WEMITCH1'), ('WEHER1'), ('WEHER3'),
    ('WEHER2'), ('WEWCM1'), ('WEWCM2'), ('WEWCM3')
)
SELECT e.employee_no AS expected_but_missing
FROM expected e
LEFT JOIN employees emp ON emp.employee_no = e.employee_no
WHERE emp.id IS NULL;

-- 2. Apply the assignments.
BEGIN;

UPDATE employees
SET attendance_group_id = '323f9122-594d-4703-be4b-08f0db7f2dd6' -- Casual (38 employees)
WHERE employee_no IN (
    'RIVERCAS10', 'CENCCAS10', 'CENCCAS9', 'CENCCAS8', 'MITCHCAS1', 'CENCCAS7', 'HERCAS1', 'WCMCAS1',
    'WCMCAS2', 'RIVERCAS9', 'WEIK3', 'WEIK2', 'WEIK1', 'SWHCAS2', 'SWHCAS1', 'RIVERCAS8',
    'RIVERCAS7', 'RIVERCAS6', 'RIVERCAS5', 'RIVERCAS4', 'RIVERCAS3', 'CENCCAS6', 'CENCCAS5', 'CENCCAS4',
    'CENCCAS3', 'CENCCAS2', 'RIVERCAS2', 'WECENC6', 'WECENC5', 'WECENC4', 'WECENC3', 'WECENC2',
    'WECENC1', 'CENCCAS1', 'WERIV3', 'RIVERCAS1', 'CVND5', 'CVND3'
);

UPDATE employees
SET attendance_group_id = 'd88f5012-86c8-4457-8157-10622c31d926' -- Permanent (70 employees)
WHERE employee_no IN (
    'MITCH1', 'MITCH2', 'MITCH11', 'MITCH10', 'MITCH13', 'MITCH6', 'MITCH12', 'CHER5',
    'CHER4', 'CHER1', 'CHER3', 'CHER6', 'CHER2', 'CHER8', 'CHER7', 'WCM5',
    'WCM6', 'WCM4', 'WCM9', 'WCM2', 'WCM8', 'WCM3', 'WCM7', 'WCM1',
    'EIK5', 'EIK2', 'EIK3', 'EIK1', 'EIK4', 'EIK6', 'EIK7', 'WESWH2',
    'WESWH6', 'WESWH1', 'SWH12', 'WESWH7', 'WESWH5', 'WESWH3', 'SWH4', 'SWH11',
    'SWH5', 'WESWH4', 'SWH2', 'SWH9', 'CENC12', 'CENC11', 'CENC10', 'CENC9',
    'CENC8', 'CENC7', 'CENC5', 'CENC4', 'CENC3', 'CENC1', 'WERIV1', 'WERIV2',
    'WERIV4', 'RIVER9', 'RIVER7', 'RIVER6', 'RIVER5', 'RIVER4', 'RIVER3', 'RIVER2',
    'RIVER1', 'CVND7', 'CVND6', 'CVND4', 'CVND2', 'CVND1'
);

UPDATE employees
SET attendance_group_id = 'c91d0edd-8505-4908-b40f-5a5e0a158609' -- Weekend (8 employees)
WHERE employee_no IN (
    'WEMITCH3', 'WEMITCH1', 'WEHER1', 'WEHER3', 'WEHER2', 'WEWCM1', 'WEWCM2', 'WEWCM3'
);

COMMIT;

-- 3. Confirm: should show 38/70/8 for Casuals/Permanent/Weekend.
SELECT ag.name, count(*) AS employee_count
FROM employees e
JOIN attendance_groups ag ON ag.id = e.attendance_group_id
GROUP BY ag.name
ORDER BY ag.name;
