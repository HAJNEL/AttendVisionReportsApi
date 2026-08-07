ALTER TABLE users
    ADD COLUMN IF NOT EXISTS photo_base64 text;
