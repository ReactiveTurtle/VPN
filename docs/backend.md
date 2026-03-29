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
- `UserPortalService`: dashboard, device revocation, IP confirmation workflow
- `AdminOperationsService`: user management, session management, audit access
- `AuditService`: audit writes

## Persistence Modes

- PostgreSQL-backed repositories when `Database:Provider = PostgreSql`
- In-memory repositories as a fallback for local or simplified runs

## API Surface

- Public request submission
- Account activation
- User auth and current session endpoints
- User dashboard and device/IP operations
- Admin request moderation
- Admin user/session/audit operations

## Current Gaps

- No end-to-end RADIUS callback/control path is implemented in the API itself.
- VPN config generation and platform-specific onboarding artifacts are not yet fully implemented.
