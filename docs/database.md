# Database

## Primary Schema

The main schema lives in `database/001_schema.sql`.

## Core Tables

- `vpn_users`
- `superadmins`
- `vpn_requests`
- `account_tokens`
- `trusted_devices`
- `trusted_ips`
- `vpn_sessions`
- `ip_change_confirmations`
- `audit_log`

## Notable Design Choices

- password hashes are stored as `TEXT`
- token hashes are stored instead of raw tokens
- device trust, IP trust, and session history are separated
- audit events are stored explicitly
- request processing is linked to admin records

## Operational Meaning

- `vpn_users` stores portal-accessible VPN users
- `superadmins` stores manually managed admin identities
- `vpn_requests` stores inbound access requests
- `account_tokens` stores activation and related token records
- `trusted_devices` and `trusted_ips` model policy state for connection trust
- `vpn_sessions` tracks active and historical sessions
- `ip_change_confirmations` supports approval flow for new IPs
- `audit_log` provides security and operations traceability
