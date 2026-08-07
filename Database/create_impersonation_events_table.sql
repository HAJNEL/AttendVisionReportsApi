CREATE TABLE IF NOT EXISTS impersonation_events (
    id uuid PRIMARY KEY,
    admin_user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    target_user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    started_at timestamp NOT NULL,
    ended_at timestamp
);

CREATE INDEX IF NOT EXISTS ix_impersonation_events_admin_open ON impersonation_events(admin_user_id) WHERE ended_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_impersonation_events_target ON impersonation_events(target_user_id);
