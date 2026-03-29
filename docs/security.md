# Security

## Implemented Controls

- Password hashing uses `Argon2` implementation in infrastructure.
- Portal auth uses cookies.
- Antiforgery protection is enabled for controllers by default.
- Activation and confirmation flows use hashed tokens with expiration.
- Audit logging exists for important account and admin actions.

## Target Credential Separation

- Portal passwords remain user-level secrets hashed with `Argon2id`.
- Target VPN access uses separate issued device credentials instead of reusing the portal password record directly.
- That separation reduces coupling between browser authentication and VPN credential lifecycle.

## Expected Operational Controls

- PostgreSQL should only be reachable from approved hosts.
- Superadmin accounts should be created manually and managed outside self-service flows.
- HTTPS termination should be enforced in deployment.
- strongSwan and FreeRADIUS should enforce network-side policy using shared data from PostgreSQL.
- Separate database roles should exist for the portal application and FreeRADIUS.
- VPN host secrets should be stored under locked-down files in `/etc/vpnportal/` or equivalent restricted paths.

## Current Risks / Gaps

- The repository models VPN trust policy, but VPN-side enforcement still depends on external integration work.
- Platform-specific device identity quality may differ across VPN clients.
- Session disconnect in the portal is currently a repository-level state change, not guaranteed real-time VPN teardown.
- The per-device VPN credential model now exists in the application, but the VPN and AAA runtime path still does not enforce it yet.
