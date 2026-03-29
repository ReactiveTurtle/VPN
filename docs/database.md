# Database

## Primary Schema

The main schema lives in `database/001_schema.sql`.

## Core Tables

- `vpn_users`
- `superadmins`
- `vpn_requests`
- `account_tokens`
- `trusted_devices`
- `vpn_device_credentials`
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

## Bootstrap Responsibilities

`PostgreSQL` is part of the VPN host bootstrap because it is a shared runtime dependency of the portal and the target AAA policy layer.

Bootstrap assets under `infrastructure/vpn-host/postgresql/` cover:

- database role creation
- database creation
- schema application through `database/001_schema.sql`
- manual superadmin seed example

## Operational Meaning

- `vpn_users` stores portal-accessible VPN users
- `superadmins` stores manually managed admin identities
- `vpn_requests` stores inbound access requests
- `account_tokens` stores activation and related token records
- `trusted_devices` and `trusted_ips` model policy state for connection trust
- `vpn_device_credentials` stores issued password-based credentials per device
- `vpn_sessions` tracks active and historical sessions
- `ip_change_confirmations` supports approval flow for new IPs
- `audit_log` provides security and operations traceability
- `vpn_sessions.session_id` is the stable accounting key used to update and close VPN sessions from internal accounting events

## Current Constraint

The repository now includes an application-side `vpn_device_credentials` table and lifecycle.

The current password-based design stores both:

- `password_hash` for application-side secure storage
- `radius_nt_hash` for `FreeRADIUS` `MSCHAPv2` validation

FreeRADIUS still needs production validation and full accounting integration to map those credentials into runtime VPN authorization.
