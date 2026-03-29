# Backend

## Layers

- `VpnPortal.Api`: HTTP entrypoints and app configuration
- `VpnPortal.Application`: DTOs and interfaces
- `VpnPortal.Domain`: core entities
- `VpnPortal.Infrastructure`: repositories and business services

## Key Services

- `RequestService`: request submission and approval flow
- `AccountActivationService`: token validation and password setup
- `AuthService`: user and superadmin authentication
- `UserPortalService`: dashboard, device credential issuance and rotation, device revocation, IP confirmation workflow
- `AdminOperationsService`: user management, session management, audit access
- `AuditService`: audit writes
- `VpnAccountingService`: internal session accounting intake for VPN-side events
- `VpnAuthEventService`: internal intake for blocked new-IP events coming from the VPN and AAA path
- `VpnOnboardingInstructionService`: platform-specific manual onboarding instructions for issued device credentials
- `VpnRuntimeControlService`: best-effort host-side runtime disconnect requests for active VPN sessions

## Persistence Modes

- PostgreSQL-backed repositories implemented through `EF Core`
- no in-memory persistence mode

## Schema Evolution

- schema evolution is handled only by `EF Core` migrations
- migrations are applied by the separate `VpnPortal.Migrations` program
- the API does not initialize or migrate the schema on startup

## API Surface

- Public request submission
- Account activation
- User auth and current session endpoints
- User dashboard and device credential/IP operations
- Admin request moderation
- Admin user/session/audit operations
- Internal accounting event intake for VPN-side session updates
- Internal auth event intake for VPN-side blocked new-IP events
- Platform-specific onboarding instructions for `iOS`, `Android`, `Windows`, and `macOS`
- Best-effort runtime disconnect requests from admin session actions

## Current Gaps

- No end-to-end RADIUS callback/control path is implemented in the API itself.
- VPN config generation and platform-specific onboarding artifacts are not yet fully implemented.
- The `max_devices` gate currently lives in the FreeRADIUS template policy, uses active device-aware session counting, and still needs live validation against real VPN accounting traffic.
- Current onboarding is manual instruction-based; `.mobileconfig`, QR, and managed client artifacts are still future work.
- The canonical host-side sender for accounting intake is `infrastructure/vpn-host/freeradius/scripts/forward-accounting-event.sh.template`.
- The canonical FreeRADIUS wiring for that sender is `infrastructure/vpn-host/freeradius/mods-available/exec-accounting.template`.
- The canonical host-side sender for blocked new-IP events is `infrastructure/vpn-host/freeradius/scripts/forward-auth-event.sh.template`.
- The canonical FreeRADIUS wiring for blocked new-IP events is `infrastructure/vpn-host/freeradius/mods-available/exec-auth.template`.
- Runtime admin disconnect currently depends on the host helper `infrastructure/vpn-host/strongswan/scripts/disconnect-session.sh.template` and still needs production validation against live `strongSwan` SAs.
