# Roadmap

## Already Implemented

- public VPN access request flow
- superadmin approval and rejection flow
- account activation with expiring token
- user and superadmin authentication
- user dashboard for devices, issued device credentials, bound device IPs, and sessions
- admin operations for users, sessions, and audit log
- PostgreSQL schema for policy and audit data
- EF Core persistence foundation and migration program
- Argon2 password hashing
- Ubuntu single-host bootstrap assets for VPN, AAA, and PostgreSQL preparation
- per-device VPN credential schema and user-side issue/rotate lifecycle
- manual platform onboarding instructions for `iOS`, `Android`, `Windows`, and `macOS`

## Partially Implemented / Modeled

- trusted device and trusted IP persistence for VPN policy
- session disconnect as application state transition
- first-connect source-IP auto-binding and explicit user-side IP unbind workflow
- documented single-host bootstrap for `strongSwan`, `FreeRADIUS`, and `PostgreSQL`
- documented separation between portal passwords and VPN device credentials

## Not Yet Fully Implemented In This Repository

- full VPN-side enforcement loop with strongSwan and FreeRADIUS
- platform-specific config generation and download UX
- one-click iOS `.mobileconfig` flow
- QR/code-driven Android onboarding flow
- end-to-end real-time disconnect on the VPN host
- richer admin and user lifecycle automation around VPN device registration
