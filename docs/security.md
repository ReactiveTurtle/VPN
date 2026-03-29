# Security

## Implemented Controls

- Password hashing uses `Argon2` implementation in infrastructure.
- Portal auth uses cookies.
- Antiforgery protection is enabled for controllers by default.
- Activation and confirmation flows use hashed tokens with expiration.
- Audit logging exists for important account and admin actions.

## Expected Operational Controls

- PostgreSQL should only be reachable from approved hosts.
- Superadmin accounts should be created manually and managed outside self-service flows.
- HTTPS termination should be enforced in deployment.
- strongSwan and FreeRADIUS should enforce network-side policy using shared data from PostgreSQL.

## Current Risks / Gaps

- The repository models VPN trust policy, but VPN-side enforcement still depends on external integration work.
- Platform-specific device identity quality may differ across VPN clients.
- Session disconnect in the portal is currently a repository-level state change, not guaranteed real-time VPN teardown.
