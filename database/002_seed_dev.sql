INSERT INTO vpn_users (id, email, username, password_hash, max_devices, active, email_confirmed, created_at, updated_at)
VALUES
    (1, 'alex@example.com', 'alex', '$argon2id$v=19$m=65536,t=3,p=1$rxaqCBytGMHXfA9JHW0Dug==$8jk57FB8d7rL95gz8krS8Zr0+hI4s/wPzblAvNlIV1A=', 2, TRUE, TRUE, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO superadmins (id, username, password_hash, created_at)
VALUES
    (1, 'rootadmin', '$argon2id$v=19$m=65536,t=3,p=1$rxaqCBytGMHXfA9JHW0Dug==$8jk57FB8d7rL95gz8krS8Zr0+hI4s/wPzblAvNlIV1A=', NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO vpn_requests (id, email, name, requested_by_ip, status, submitted_at)
VALUES
    (1, 'pending.user@example.com', 'Pending User', '203.0.113.24', 'pending', NOW() - INTERVAL '2 days'),
    (2, 'approved.user@example.com', 'Approved User', '198.51.100.10', 'approved', NOW() - INTERVAL '3 days')
ON CONFLICT (id) DO NOTHING;

UPDATE vpn_requests
SET admin_comment = 'Approved for internal testing',
    processed_at = NOW() - INTERVAL '3 days' + INTERVAL '30 minutes'
WHERE id = 2;

INSERT INTO trusted_devices (id, user_id, device_uuid, device_name, device_type, platform, status, first_seen_at, last_seen_at, approved_at)
VALUES
    (1, 1, 'ios-alex-001', 'Alex iPhone', 'phone', 'ios', 'active', NOW() - INTERVAL '1 day', NOW(), NOW() - INTERVAL '1 day')
ON CONFLICT (id) DO NOTHING;

INSERT INTO trusted_ips (id, user_id, device_id, ip_address, status, first_seen_at, last_seen_at, approved_at)
VALUES
    (1, 1, 1, '203.0.113.50', 'active', NOW() - INTERVAL '1 day', NOW(), NOW() - INTERVAL '1 day')
ON CONFLICT (id) DO NOTHING;

INSERT INTO account_tokens (id, user_email, token_hash, purpose, expires_at, used, created_at)
VALUES
    (1, 'approved.user@example.com', '7c43ef5ae21d43ce2743f770c68e24def1a43ee2f416d2438410c8af7af2ff2c', 'account_activation', NOW() + INTERVAL '1 day', FALSE, NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO ip_change_confirmations (id, user_id, device_id, requested_ip, token_hash, status, expires_at, created_at)
VALUES
    (1, 1, 1, '198.51.100.77', 'pending-ip-demo-hash', 'pending', NOW() + INTERVAL '1 hour', NOW())
ON CONFLICT (id) DO NOTHING;

INSERT INTO vpn_sessions (id, user_id, device_id, source_ip, assigned_vpn_ip, session_id, started_at, last_seen_at, active, authorized)
VALUES
    (1, 1, 1, '203.0.113.50', '10.10.0.12', 'seed-session-001', NOW() - INTERVAL '35 minutes', NOW(), TRUE, TRUE)
ON CONFLICT (id) DO NOTHING;

SELECT setval('vpn_users_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM vpn_users), 1));
SELECT setval('superadmins_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM superadmins), 1));
SELECT setval('vpn_requests_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM vpn_requests), 1));
SELECT setval('trusted_devices_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM trusted_devices), 1));
SELECT setval('trusted_ips_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM trusted_ips), 1));
SELECT setval('account_tokens_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM account_tokens), 1));
SELECT setval('ip_change_confirmations_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM ip_change_confirmations), 1));
SELECT setval('vpn_sessions_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM vpn_sessions), 1));
