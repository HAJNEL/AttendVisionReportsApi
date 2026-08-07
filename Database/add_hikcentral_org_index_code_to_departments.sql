ALTER TABLE departments
    ADD COLUMN IF NOT EXISTS hikcentral_org_index_code text;
