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
- Password-based VPN device credentials require storing additional password material for the AAA path, including an `NT hash` for `MSCHAPv2` validation.

## Expected Operational Controls

- PostgreSQL should only be reachable from approved hosts.
- Superadmin accounts should be created manually and managed outside self-service flows.
- The first superadmin should be created only after EF migrations are applied, using the documented manual procedure.
- HTTPS termination should be enforced in deployment.
- strongSwan and FreeRADIUS should enforce network-side policy using shared data from PostgreSQL.
- Separate database roles should exist for the portal application and FreeRADIUS.
- VPN host secrets should be stored under locked-down files in `/etc/vpnportal/` or equivalent restricted paths.
- `FreeRADIUS` should reject inactive users and new sessions beyond `max_devices` before VPN tunnel establishment.
- The current `max_devices` policy is device-aware: it counts other active device sessions rather than blindly rejecting a reconnect from the same device credential.
- The current trusted-IP policy is also enforced in FreeRADIUS: once a device has active trusted IPs, a new source IP is rejected until it is confirmed through the portal flow.
- Runtime disconnect is best-effort and currently depends on matching live `strongSwan` SAs from host-side helper logic.

## Current Risks / Gaps

- The repository models VPN trust policy, but VPN-side enforcement still depends on external integration work.
- Platform-specific device identity quality may differ across VPN clients.
- Session disconnect in the portal is currently a repository-level state change, not guaranteed real-time VPN teardown.
- The per-device VPN credential model now exists in the application, but the VPN and AAA runtime path still needs production validation and accounting integration.
- The blocked new-IP flow is now wired end-to-end in templates and API shape, but still needs production validation with live FreeRADIUS traffic.
- Admin disconnect now has a host-side runtime path, but it still needs production validation against live `swanctl --list-sas --raw` output on the target host.

## First Superadmin

Use `docs/runbooks/create-first-superadmin.md` to create the first `superadmin` manually on a trusted server.
