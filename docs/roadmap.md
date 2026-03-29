# Roadmap

## Already Implemented

- public VPN access request flow
- superadmin approval and rejection flow
- account activation with expiring token
- user and superadmin authentication
- user dashboard for devices, trusted IPs, confirmations, and sessions
- admin operations for users, sessions, and audit log
- PostgreSQL schema for policy and audit data
- Argon2 password hashing

## Partially Implemented / Modeled

- trusted device and trusted IP persistence for VPN policy
- session disconnect as application state transition
- email-driven IP confirmation workflow

## Not Yet Fully Implemented In This Repository

- full VPN-side enforcement loop with strongSwan and FreeRADIUS
- platform-specific config generation and download UX
- one-click iOS `.mobileconfig` flow
- QR/code-driven Android onboarding flow
- end-to-end real-time disconnect on the VPN host
- richer admin and user lifecycle automation around VPN device registration
