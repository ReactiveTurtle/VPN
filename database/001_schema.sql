CREATE TABLE IF NOT EXISTS vpn_users (
    id BIGSERIAL PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    username VARCHAR(64) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    max_devices INT NOT NULL DEFAULT 2 CHECK (max_devices > 0),
    active BOOLEAN NOT NULL DEFAULT TRUE,
    email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ NULL,
    deactivated_at TIMESTAMPTZ NULL
);

CREATE TABLE IF NOT EXISTS superadmins (
    id BIGSERIAL PRIMARY KEY,
    username VARCHAR(64) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMPTZ NULL
);

CREATE TABLE IF NOT EXISTS vpn_requests (
    id BIGSERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    name VARCHAR(255) NULL,
    requested_by_ip INET NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'approved', 'rejected')),
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ NULL,
    processed_by_admin_id BIGINT NULL REFERENCES superadmins(id),
    approved_user_id BIGINT NULL REFERENCES vpn_users(id),
    admin_comment TEXT NULL
);

CREATE INDEX IF NOT EXISTS ix_vpn_requests_email ON vpn_requests(email);
CREATE INDEX IF NOT EXISTS ix_vpn_requests_status_submitted_at ON vpn_requests(status, submitted_at DESC);

CREATE TABLE IF NOT EXISTS account_tokens (
    id BIGSERIAL PRIMARY KEY,
    user_email VARCHAR(255) NOT NULL,
    token_hash TEXT NOT NULL UNIQUE,
    purpose VARCHAR(32) NOT NULL CHECK (purpose IN ('account_activation', 'ip_confirmation', 'password_reset')),
    expires_at TIMESTAMPTZ NOT NULL,
    used BOOLEAN NOT NULL DEFAULT FALSE,
    used_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_admin_id BIGINT NULL REFERENCES superadmins(id)
);

CREATE INDEX IF NOT EXISTS ix_account_tokens_user_email_purpose ON account_tokens(user_email, purpose);

CREATE TABLE IF NOT EXISTS trusted_devices (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES vpn_users(id) ON DELETE CASCADE,
    device_uuid VARCHAR(128) NOT NULL,
    device_name VARCHAR(255) NULL,
    device_type VARCHAR(32) NOT NULL,
    platform VARCHAR(32) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'active', 'revoked')),
    first_seen_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_seen_at TIMESTAMPTZ NULL,
    approved_at TIMESTAMPTZ NULL,
    revoked_at TIMESTAMPTZ NULL,
    UNIQUE (user_id, device_uuid)
);

CREATE INDEX IF NOT EXISTS ix_trusted_devices_user_id_status ON trusted_devices(user_id, status);

CREATE TABLE IF NOT EXISTS trusted_ips (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES vpn_users(id) ON DELETE CASCADE,
    device_id BIGINT NULL REFERENCES trusted_devices(id) ON DELETE SET NULL,
    ip_address INET NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active' CHECK (status IN ('pending', 'active', 'revoked')),
    first_seen_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_seen_at TIMESTAMPTZ NULL,
    approved_at TIMESTAMPTZ NULL,
    revoked_at TIMESTAMPTZ NULL,
    UNIQUE (user_id, ip_address)
);

CREATE INDEX IF NOT EXISTS ix_trusted_ips_user_id_status ON trusted_ips(user_id, status);

CREATE TABLE IF NOT EXISTS vpn_sessions (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES vpn_users(id) ON DELETE CASCADE,
    device_id BIGINT NULL REFERENCES trusted_devices(id) ON DELETE SET NULL,
    source_ip INET NOT NULL,
    assigned_vpn_ip INET NULL,
    nas_identifier VARCHAR(128) NULL,
    session_id VARCHAR(128) NULL,
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_seen_at TIMESTAMPTZ NULL,
    ended_at TIMESTAMPTZ NULL,
    termination_reason VARCHAR(64) NULL,
    active BOOLEAN NOT NULL DEFAULT TRUE,
    authorized BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS ix_vpn_sessions_user_id_started_at ON vpn_sessions(user_id, started_at DESC);
CREATE INDEX IF NOT EXISTS ix_vpn_sessions_active ON vpn_sessions(active) WHERE active = TRUE;

CREATE TABLE IF NOT EXISTS ip_change_confirmations (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES vpn_users(id) ON DELETE CASCADE,
    device_id BIGINT NULL REFERENCES trusted_devices(id) ON DELETE SET NULL,
    requested_ip INET NOT NULL,
    token_hash TEXT NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'expired', 'cancelled')),
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    confirmed_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_ip_change_confirmations_user_id_status ON ip_change_confirmations(user_id, status);

CREATE TABLE IF NOT EXISTS audit_log (
    id BIGSERIAL PRIMARY KEY,
    actor_type VARCHAR(20) NOT NULL CHECK (actor_type IN ('user', 'superadmin', 'system')),
    actor_id BIGINT NULL,
    action VARCHAR(64) NOT NULL,
    entity_type VARCHAR(64) NOT NULL,
    entity_id VARCHAR(64) NOT NULL,
    ip_address INET NULL,
    details JSONB NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_audit_log_created_at ON audit_log(created_at DESC);
