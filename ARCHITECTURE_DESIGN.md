# VPN Portal Architecture and Design Document

## 1. Document purpose

This document describes the target architecture, component responsibilities, data model, security controls, integration contracts, deployment model, and implementation approach for a centralized VPN access platform.

The system provides:

- a public portal for VPN access requests;
- an administrative portal for approvals and operational control;
- centralized authentication and authorization for VPN access;
- device and IP binding with approval workflows;
- secure delivery of onboarding instructions and client configuration.

## 2. Goals and non-goals

### 2.1 Goals

- Provide a secure VPN onboarding flow with admin approval.
- Centralize user lifecycle, device policy, and session oversight.
- Support `strongSwan` IKEv2 as the VPN endpoint.
- Use `FreeRADIUS + PostgreSQL` for AAA and policy checks.
- Implement a web portal on `.NET 10` with `ASP.NET Core Web API` and `Angular SPA`.
- Store passwords using `Argon2id`.
- Support one-time account activation links valid for 24 hours.
- Enforce per-user device limits and unknown IP/device approval.

### 2.2 Non-goals

- Building a generic IAM platform.
- Supporting every VPN protocol; the primary target is IKEv2 through `strongSwan`.
- Providing self-service superadmin registration.
- Exposing database access outside secured server-side administration.

## 3. High-level architecture

### 3.1 Main components

1. `Angular SPA`
   - User portal.
   - Admin portal.
   - Talks only to backend API over HTTPS.

2. `.NET 10 ASP.NET Core Web API`
   - Public request submission.
   - Account activation and password setup.
   - User authentication for portal access.
   - Admin workflows.
   - Device/IP approval flows.
   - Config and instruction generation.
   - Email notifications.

3. `PostgreSQL`
   - Source of truth for portal users, admins, requests, tokens, devices, IP approvals, audit, and session records.
   - Shared policy data for portal and RADIUS checks.

4. `FreeRADIUS`
   - AAA entry point for VPN authentication requests.
   - Verifies username/password against hashes stored in PostgreSQL or via portal-owned auth function.
   - Applies device/IP policy and user active state checks.
   - Writes accounting/session events.

5. `strongSwan`
   - IKEv2 VPN server.
   - Delegates authentication/accounting to RADIUS.

6. `SMTP provider`
   - Sends approval links, IP confirmation links, and optional security alerts.

### 3.2 Recommended deployment topology

- `Reverse proxy` (`nginx` or similar) in front of ASP.NET Core.
- `Angular` served either by reverse proxy as static assets or by ASP.NET Core.
- `ASP.NET Core API`, `FreeRADIUS`, and `strongSwan` on private network segments.
- `PostgreSQL` reachable only from application/VPN hosts.
- Admin access to servers only through SSH from restricted IPs or through a management VPN.

### 3.3 Logical request flow

1. User submits request in SPA.
2. API stores request in PostgreSQL.
3. Superadmin reviews request in admin SPA.
4. API approves request, creates activation token, sends email.
5. User activates account, sets password, receives setup instructions.
6. User connects to `strongSwan`.
7. `strongSwan` calls `FreeRADIUS`.
8. `FreeRADIUS` validates credentials and policy via PostgreSQL.
9. If device/IP is trusted, access is allowed; otherwise access is denied and approval workflow is triggered.
10. Accounting/session events are written back for admin visibility.

## 4. Detailed component design

### 4.1 Frontend: Angular SPA

Modules:

- Public request form.
- Activation/password setup flow.
- User dashboard:
  - profile summary;
  - trusted devices;
  - trusted IPs;
  - active sessions;
  - download instructions/config for platform.
- Admin dashboard:
  - request moderation;
  - users;
  - sessions;
  - device/IP approvals;
  - audit view.

Security:

- HTTPS only.
- Secure cookie-based auth preferred for portal sessions.
- CSRF protection for cookie-authenticated API.
- Strict CSP, X-Frame-Options, HSTS.

### 4.2 Backend: ASP.NET Core Web API

Suggested layers:

- `API` layer: controllers/endpoints.
- `Application` layer: workflows, validation, orchestration.
- `Domain` layer: business rules.
- `Infrastructure` layer: PostgreSQL, SMTP, token generation, password hashing, audit logging.

Core services:

- `RequestService`
- `AccountActivationService`
- `PasswordHashingService`
- `UserManagementService`
- `AdminModerationService`
- `DeviceTrustService`
- `IpApprovalService`
- `SessionService`
- `EmailNotificationService`
- `ConfigInstructionService`
- `AuditService`

### 4.3 FreeRADIUS integration

Responsibilities:

- Accept VPN auth requests from `strongSwan`.
- Validate credentials.
- Enforce `active = true`.
- Enforce `max_devices`.
- Check trusted device and trusted IP policies.
- Log accepts/rejects/accounting events.

Recommended approach:

- Use SQL-backed policy lookups in PostgreSQL.
- Store device identifier from client certificate identity, EAP identity, or a stable client-supplied identifier where technically available.
- If the VPN protocol/client cannot reliably provide a device UUID, treat device binding as a combination of platform metadata, account, and approved connection fingerprint.

Important constraint:

- Native IKEv2 does not always provide a strong hardware-bound device UUID to RADIUS. The implementation must define what is considered a "device" for each platform. For example:
  - iOS/macOS: profile-bound identifier or generated portal registration ID.
  - Android: app/import-specific registration ID.
  - Windows: machine-bound identifier may be limited; fallback to portal-issued device registration plus IP confirmation.

### 4.4 strongSwan

Responsibilities:

- IKEv2 tunnel termination.
- RADIUS auth/accounting delegation.
- IP lease assignment.
- Session disconnect on admin-triggered revocation, if supported by deployed control path.

Notes:

- If real-time disconnect is required, include a plan for `CoA/Disconnect-Request` support or terminate sessions by disabling account and dropping CHILD_SA/IKESA on the VPN host.

### 4.5 PostgreSQL

Responsibilities:

- Persistent storage.
- Policy lookup for portal and AAA.
- Audit and reporting.

Requirements:

- TLS for DB connections if split across hosts.
- Least-privilege database users:
  - app user;
  - radius user;
  - migration user.

## 5. Data model

The initial schema from the requirement is a good starting point, but it should be expanded for production use.

### 5.1 Key design changes

- Use `TEXT` for password hashes instead of `VARCHAR(128)`.
- Store token hashes, not raw tokens.
- Separate trusted devices, trusted IPs, and active/history sessions.
- Add audit logging.
- Add explicit linkage between request approval actions and superadmins.
- Add portal sessions or refresh token tracking if using JWT/refresh tokens.

### 5.2 Proposed tables

#### `vpn_users`

- `id BIGSERIAL PRIMARY KEY`
- `email VARCHAR(255) UNIQUE NOT NULL`
- `username VARCHAR(64) UNIQUE NOT NULL`
- `password_hash TEXT NOT NULL`
- `max_devices INT NOT NULL DEFAULT 2`
- `active BOOLEAN NOT NULL DEFAULT TRUE`
- `email_confirmed BOOLEAN NOT NULL DEFAULT FALSE`
- `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `last_login_at TIMESTAMPTZ NULL`
- `deactivated_at TIMESTAMPTZ NULL`

#### `superadmins`

- `id BIGSERIAL PRIMARY KEY`
- `username VARCHAR(64) UNIQUE NOT NULL`
- `password_hash TEXT NOT NULL`
- `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `last_login_at TIMESTAMPTZ NULL`

#### `vpn_requests`

- `id BIGSERIAL PRIMARY KEY`
- `email VARCHAR(255) NOT NULL`
- `name VARCHAR(255) NULL`
- `requested_by_ip INET NULL`
- `status VARCHAR(20) NOT NULL DEFAULT 'pending'`
- `submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `processed_at TIMESTAMPTZ NULL`
- `processed_by_admin_id BIGINT NULL REFERENCES superadmins(id)`
- `approved_user_id BIGINT NULL REFERENCES vpn_users(id)`
- `admin_comment TEXT NULL`

#### `account_tokens`

- `id BIGSERIAL PRIMARY KEY`
- `user_email VARCHAR(255) NOT NULL`
- `token_hash TEXT NOT NULL UNIQUE`
- `purpose VARCHAR(32) NOT NULL`
- `expires_at TIMESTAMPTZ NOT NULL`
- `used BOOLEAN NOT NULL DEFAULT FALSE`
- `used_at TIMESTAMPTZ NULL`
- `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `created_by_admin_id BIGINT NULL REFERENCES superadmins(id)`

#### `trusted_devices`

- `id BIGSERIAL PRIMARY KEY`
- `user_id BIGINT NOT NULL REFERENCES vpn_users(id)`
- `device_uuid VARCHAR(128) NOT NULL`
- `device_name VARCHAR(255) NULL`
- `device_type VARCHAR(32) NOT NULL`
- `platform VARCHAR(32) NOT NULL`
- `status VARCHAR(20) NOT NULL DEFAULT 'active'`
- `first_seen_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `last_seen_at TIMESTAMPTZ NULL`
- `approved_at TIMESTAMPTZ NULL`
- `revoked_at TIMESTAMPTZ NULL`

Unique recommendation:

- unique index on `(user_id, device_uuid)`

#### `trusted_ips`

- `id BIGSERIAL PRIMARY KEY`
- `user_id BIGINT NOT NULL REFERENCES vpn_users(id)`
- `device_id BIGINT NULL REFERENCES trusted_devices(id)`
- `ip_address INET NOT NULL`
- `status VARCHAR(20) NOT NULL DEFAULT 'active'`
- `first_seen_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `last_seen_at TIMESTAMPTZ NULL`
- `approved_at TIMESTAMPTZ NULL`
- `revoked_at TIMESTAMPTZ NULL`

Unique recommendation:

- unique index on `(user_id, ip_address)`

#### `vpn_sessions`

- `id BIGSERIAL PRIMARY KEY`
- `user_id BIGINT NOT NULL REFERENCES vpn_users(id)`
- `device_id BIGINT NULL REFERENCES trusted_devices(id)`
- `source_ip INET NOT NULL`
- `assigned_vpn_ip INET NULL`
- `nas_identifier VARCHAR(128) NULL`
- `session_id VARCHAR(128) NULL`
- `started_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `last_seen_at TIMESTAMPTZ NULL`
- `ended_at TIMESTAMPTZ NULL`
- `termination_reason VARCHAR(64) NULL`
- `active BOOLEAN NOT NULL DEFAULT TRUE`
- `authorized BOOLEAN NOT NULL DEFAULT TRUE`

#### `ip_change_confirmations`

- `id BIGSERIAL PRIMARY KEY`
- `user_id BIGINT NOT NULL REFERENCES vpn_users(id)`
- `device_id BIGINT NULL REFERENCES trusted_devices(id)`
- `requested_ip INET NOT NULL`
- `token_hash TEXT NOT NULL UNIQUE`
- `status VARCHAR(20) NOT NULL DEFAULT 'pending'`
- `expires_at TIMESTAMPTZ NOT NULL`
- `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
- `confirmed_at TIMESTAMPTZ NULL`

#### `audit_log`

- `id BIGSERIAL PRIMARY KEY`
- `actor_type VARCHAR(20) NOT NULL`
- `actor_id BIGINT NULL`
- `action VARCHAR(64) NOT NULL`
- `entity_type VARCHAR(64) NOT NULL`
- `entity_id VARCHAR(64) NOT NULL`
- `ip_address INET NULL`
- `details JSONB NULL`
- `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`

## 6. Authentication and authorization design

### 6.1 Portal authentication

Users:

- Login with username/email + password.
- Password verified with `Argon2id`.
- Session issued via secure HTTP-only cookie.

Superadmins:

- Separate login endpoint and role-based authorization.
- Records in `superadmins` only.
- Creation only through direct database insert or controlled bootstrap tool.

### 6.2 VPN authentication

- Primary credential: username + password.
- Password checked against `Argon2id` hash.
- Access granted only if:
  - user exists;
  - account is active;
  - device policy passes;
  - IP policy passes;
  - device limit is not exceeded.

### 6.3 Authorization model

Roles:

- `User`
- `SuperAdmin`

Permissions:

- `User`: own profile, own devices, own sessions, own IP approvals, own onboarding instructions.
- `SuperAdmin`: all moderation and operational management features.

## 7. Security design

### 7.1 Password hashing

Standard:

- `Argon2id`
- Recommended baseline parameters:
  - memory: `65536 KB`;
  - iterations: `3`;
  - parallelism: `1-2` depending on host profile;
  - salt: `16 bytes` minimum;
  - hash length: `32 bytes` or more.

Store as a self-describing string, for example PHC-like format:

`$argon2id$v=19$m=65536,t=3,p=1$<salt_base64>$<hash_base64>`

### 7.2 Token security

- Generate cryptographically secure random tokens.
- Store only token hashes in DB.
- Compare using constant-time equality.
- Token TTL: 24 hours for account activation; 15-60 minutes recommended for IP confirmation.

### 7.3 Transport security

- HTTPS everywhere for portal.
- HSTS enabled.
- TLS for SMTP where supported.
- Restrict DB to private network.

### 7.4 Application hardening

- Input validation for all endpoints.
- Rate limiting for request submission, login, activation, and IP approval endpoints.
- Account lockout or escalating delays on repeated failures.
- Centralized audit trail.
- Secure secrets storage via environment variables or secret manager.

### 7.5 Admin security

- Separate admin login UI and API routes.
- Strong password policy.
- Prefer MFA in a later phase.
- Restrict admin portal by IP if operationally acceptable.

## 8. Business workflows

### 8.1 User request submission

1. User opens public form.
2. Enters email and optional name.
3. API validates input and creates `vpn_requests` row.
4. Admin sees request as `pending`.

Failure/abuse controls:

- rate limit by IP and email;
- optional CAPTCHA;
- deduplicate repeated pending requests for the same email.

### 8.2 Request approval

1. Superadmin reviews pending request.
2. Superadmin approves or rejects.
3. If approved:
   - create `vpn_users` row if absent;
   - create activation token;
   - send email with one-time link.
4. If rejected:
   - mark request rejected;
   - optionally notify requester.

### 8.3 Account activation

1. User clicks one-time link.
2. API validates token hash, TTL, and unused state.
3. User sets password.
4. API hashes password using `Argon2id`.
5. Token marked used.
6. User is redirected to onboarding page.

### 8.4 Initial device registration

1. User selects platform.
2. Portal issues setup instructions and, where applicable, generated profile/config.
3. First successful or first attempted connection establishes a candidate device record.
4. If policy allows first device auto-enrollment, mark device trusted.
5. Otherwise require user confirmation.

Recommended policy:

- auto-approve first device only after account activation;
- further devices require slot availability and optionally portal-side confirmation.

### 8.5 New IP detection and approval

1. Connection arrives from trusted or candidate device but unknown IP.
2. RADIUS denies access.
3. API or integration process creates `ip_change_confirmations` row.
4. Email sent with confirmation link.
5. User confirms new IP.
6. `trusted_ips` updated.
7. User reconnects successfully.

### 8.6 Device limit enforcement

1. RADIUS/API counts active trusted devices for user.
2. If count >= `max_devices` and incoming device is new, deny access.
3. User may delete/revoke an old device in portal.
4. New device can then be approved.

### 8.7 Admin forced disconnect

1. Superadmin identifies active session.
2. API marks session for termination and disables account or session authorization state.
3. Infrastructure triggers disconnect path if available.
4. Session record updated when accounting stop arrives.

## 9. Platform-specific onboarding

### 9.1 iOS

- Preferred output: `.mobileconfig`.
- One-click profile install.
- Include server address, remote ID, username, and auth instructions.

### 9.2 Android

- If native IKEv2 is used, provide step-by-step instructions.
- If a managed client is used, provide importable config or QR-based bootstrap where supported.

### 9.3 Windows

- Provide IKEv2 instructions.
- If supported operationally, provide PowerShell-assisted setup package.

### 9.4 macOS

- Provide native IKEv2 instructions or Apple profile when practical.

Important note:

- The requirement mentions `.ovpn`, but that format belongs to OpenVPN, not `strongSwan IKEv2`. For this architecture, use IKEv2-native instructions/profile artifacts instead.

## 10. API design

### 10.1 Public endpoints

- `POST /api/requests`
- `GET /api/account/activate?token=...`
- `POST /api/account/activate`
- `POST /api/auth/login`
- `POST /api/auth/logout`

### 10.2 User endpoints

- `GET /api/me`
- `GET /api/me/devices`
- `DELETE /api/me/devices/{id}`
- `GET /api/me/sessions`
- `GET /api/me/platforms`
- `GET /api/me/platforms/{platform}/instructions`
- `POST /api/me/ip-confirmations/{token}/confirm`

### 10.3 Admin endpoints

- `GET /api/admin/requests`
- `POST /api/admin/requests/{id}/approve`
- `POST /api/admin/requests/{id}/reject`
- `GET /api/admin/users`
- `GET /api/admin/users/{id}`
- `PATCH /api/admin/users/{id}`
- `POST /api/admin/users/{id}/deactivate`
- `POST /api/admin/users/{id}/activate`
- `GET /api/admin/sessions`
- `POST /api/admin/sessions/{id}/disconnect`
- `GET /api/admin/audit`

### 10.4 Internal/integration endpoints

If required, expose internal-only endpoints for RADIUS integration or event handling, protected by mTLS or network isolation.

Examples:

- `POST /internal/radius/accounting-events`

## 11. Database access model

### 11.1 Application DB user

Permissions:

- CRUD on portal tables.
- No superuser privileges.

### 11.2 RADIUS DB user

Permissions:

- Read on users/policy tables.
- Insert/update only where accounting/session logging requires it.
- No schema migration rights.

### 11.3 Migration DB user

- Used only in deployment pipelines or manual maintenance windows.

## 12. Operational design

### 12.1 Logging

Collect structured logs from:

- API;
- reverse proxy;
- FreeRADIUS;
- strongSwan.

Log categories:

- auth success/failure;
- request moderation;
- activation token use;
- IP approval events;
- device revocation;
- session disconnects;
- admin actions.

### 12.2 Metrics

Recommended metrics:

- active VPN sessions;
- failed auth count;
- new IP approval attempts;
- pending requests;
- email send failures;
- DB latency;
- API latency and error rates.

### 12.3 Audit and retention

- Keep security-relevant audit trails for at least 90-365 days according to policy.
- Define retention for sessions and rejected auth attempts separately.

## 13. Deployment design

### 13.1 Environments

- `dev`
- `staging`
- `prod`

### 13.2 Runtime layout

Recommended split:

- Host 1: reverse proxy + SPA + ASP.NET Core API
- Host 2: PostgreSQL
- Host 3: strongSwan + FreeRADIUS

Small-scale alternative:

- API and SPA on one host;
- VPN/RADIUS on another host;
- PostgreSQL on dedicated host or managed service.

### 13.3 Secrets management

- SMTP credentials;
- DB credentials;
- app encryption keys;
- cookie signing keys;
- VPN shared secrets/certs as applicable.

Never store secrets in source control.

## 14. Suggested implementation phases

### Phase 1: Core onboarding

- Public request form.
- Admin moderation.
- Account activation.
- Password hashing.
- User login.

### Phase 2: VPN policy integration

- FreeRADIUS auth against PostgreSQL.
- User active state checks.
- Session logging.

### Phase 3: Device/IP trust model

- Trusted devices.
- Trusted IPs.
- New IP confirmation workflow.
- User self-service device revocation.

### Phase 4: Admin operations

- Session list.
- Forced disconnect.
- Audit view.
- Limits and deactivation management.

### Phase 5: Hardening and observability

- Rate limits.
- dashboards/alerts.
- backup/restore drills.
- MFA for admins.

## 15. Risks and constraints

### 15.1 Device identity limitations

- Native IKEv2 clients do not always expose a reliable hardware identifier.
- Final design must define acceptable device identity semantics per platform.

### 15.2 Real-time disconnect complexity

- Disconnecting existing VPN sessions may require protocol- and server-specific integration beyond simply marking a DB row.

### 15.3 UX differences by platform

- iOS/macOS profile delivery is smoother than Windows/Android native flows.
- User instructions must be platform-specific.

### 15.4 Email dependency

- IP approval and activation rely on timely email delivery.
- Deliverability monitoring is required.

## 16. Recommended final technology decisions

- Frontend: `Angular SPA`
- Backend: `.NET 10 ASP.NET Core Web API`
- Database: `PostgreSQL`
- VPN: `strongSwan IKEv2`
- AAA: `FreeRADIUS + PostgreSQL`
- Password hashing: `Argon2id`
- Email: `SMTP` or `SendGrid`
- Auth for portal: secure cookies with ASP.NET Core Identity-style patterns or custom auth with strict session management

## 17. Deliverables for implementation stage

- Production PostgreSQL schema and migrations.
- ASP.NET Core solution skeleton with API/auth modules.
- Angular SPA skeleton with user/admin areas.
- SMTP templates for activation and IP confirmation.
- FreeRADIUS SQL policy design.
- strongSwan integration configuration.
- Operations runbook and backup/restore instructions.

## 18. Acceptance criteria

- User can submit request.
- Superadmin can approve/reject request.
- Approved user receives a one-time link valid for 24 hours.
- User can set password and log in to portal.
- VPN authentication checks credentials and active status.
- New device/IP attempts are blocked until approved per policy.
- User can revoke old device to free a slot.
- Superadmin can review sessions and deactivate users.
- Security-relevant actions are audited.
