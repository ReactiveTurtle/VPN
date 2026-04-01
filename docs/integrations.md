# Integrations

## strongSwan

- Intended as the IKEv2 VPN endpoint.
- Expected to delegate authentication and accounting to FreeRADIUS.
- Bootstrap templates for the Ubuntu host live under `infrastructure/vpn-host/strongswan/`.

## FreeRADIUS

- Intended as the AAA layer.
- Expected to validate credentials and enforce trust/device policy through PostgreSQL-backed data.
- Bootstrap templates for the Ubuntu host live under `infrastructure/vpn-host/freeradius/`.

## PostgreSQL

- Source of truth for portal state and VPN policy data.
- Used by the application through the EF Core persistence layer and expected to support VPN-side lookups.
- Host bootstrap scripts install PostgreSQL and create separate app and RADIUS roles.
- Schema changes are applied by the separate `VpnPortal.Migrations` program.

## Email

- SMTP is the production-oriented delivery path.
- Console email service exists as a fallback for development and simplified environments.

## Integration Status

- Portal-side persistence and workflow logic are present.
- External VPN and AAA server configuration is now represented operationally in this repository under `infrastructure/vpn-host/`.
- Application-side per-device VPN credentials now exist.
- FreeRADIUS bootstrap templates now resolve `NT-Password` from `vpn_device_credentials` for password-based device credentials.
- FreeRADIUS bootstrap templates now also gate access by user `active` state and `max_devices` using PostgreSQL-backed policy checks.
- The current `max_devices` check counts other active devices for the same user, so reconnecting the same device does not consume an extra slot by itself.
- The API now exposes an internal accounting endpoint for VPN-side session updates.
- The VPN host bootstrap now installs `/usr/local/lib/vpnportal/forward-accounting-event.sh` as the canonical helper for forwarding accounting events to the internal API.
- FreeRADIUS bootstrap now enables an `exec-accounting` module that invokes the canonical forwarder on `Start`, `Interim-Update`, and `Stop` accounting events.
- Session accounting is keyed by `session_id`, which is expected to be unique when present.
- The portal now serves platform-specific manual onboarding instructions for `iOS`, `Android`, `Windows`, and `macOS` based on `VpnAccess:ServerAddress`.
- Admin disconnect now has a best-effort host-side runtime path through a local `strongSwan` helper script.
- End-to-end production validation of the AAA path is still pending.

## Target Runtime Flow

1. User requests and activates access through the portal.
2. User signs in to the portal with the portal password.
3. User receives or rotates a VPN device credential for a specific device.
4. The first successful device connection is allowed even without a bound IP, and `strongSwan` delegates AAA to `FreeRADIUS`.
5. `FreeRADIUS` validates the device credential against PostgreSQL-backed policy data, including `active`, device-bound source IP, and `max_devices` checks.
6. `FreeRADIUS` accounting hooks invoke the canonical host-side forwarder, which posts `Start`, `Interim-Update`, and `Stop` events into the internal API to update `vpn_sessions` and auto-bind the first source IP to the device.
7. If the same device later attempts to connect from a different IP, `FreeRADIUS` rejects that attempt until the user unbinds the old IP in the portal.
8. Admin disconnect can request a host-side `strongSwan` teardown through the configured runtime helper script.

## Current Operational Contract

- Host bootstrap env files: `/etc/vpnportal/vpn-host.prod.env`, `/etc/vpnportal/vpn-host.stage.env`
- App runtime env files: `/etc/vpnportal/vpnportal.prod.container.env`, `/etc/vpnportal/vpnportal.stage.container.env`
- Shared app-facing values in the bootstrap env files use the runtime config names directly, for example `Email__PublicBaseUrl`, `InternalApi__SharedSecret`, `VpnAccess__ServerAddress`, `VpnRuntime__DisconnectScriptPath`, and `Email__*`.
- Internal endpoint: `POST /api/internal/radius/accounting-events`
- Auth header: `X-Internal-Api-Key: <InternalApi:SharedSecret>`
- Canonical host-side helper: `/usr/local/lib/vpnportal/forward-accounting-event.sh`
- Canonical FreeRADIUS module file: `/etc/freeradius/3.0/mods-available/exec-accounting`
- Canonical strongSwan runtime disconnect helper: `/usr/local/lib/vpnportal/disconnect-session.sh`

## Verification Runbook

- Non-destructive smoke check: `deploy/predeploy/infrastructure/vpn-host/08-smoke-test-portal.sh`
- End-to-end server validation guide: `infrastructure/vpn-host/runbooks/verify-vpn-runtime-flow.md`
