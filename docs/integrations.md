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
- Used by the application directly and expected to support VPN-side lookups.
- Host bootstrap scripts install PostgreSQL and create separate app and RADIUS roles.

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
- The API now also exposes an internal auth-event endpoint for blocked new-IP/security events.
- The VPN host bootstrap now installs `/usr/local/lib/vpnportal/forward-accounting-event.sh` as the canonical helper for forwarding accounting events to the internal API.
- The VPN host bootstrap now also installs `/usr/local/lib/vpnportal/forward-auth-event.sh` as the canonical helper for forwarding blocked new-IP events to the internal API.
- FreeRADIUS bootstrap now enables an `exec-accounting` module that invokes the canonical forwarder on `Start`, `Interim-Update`, and `Stop` accounting events.
- FreeRADIUS bootstrap now also enables an `exec-auth` module that forwards blocked new-IP attempts into the portal confirmation flow.
- Session accounting is keyed by `session_id`, which is expected to be unique when present.
- The portal now serves platform-specific manual onboarding instructions for `iOS`, `Android`, `Windows`, and `macOS` based on `VpnAccess:ServerAddress`.
- End-to-end production validation of the AAA path is still pending.

## Target Runtime Flow

1. User requests and activates access through the portal.
2. User signs in to the portal with the portal password.
3. User receives or rotates a VPN device credential for a specific device.
4. `strongSwan` accepts the IKEv2 connection and delegates AAA to `FreeRADIUS`.
5. `FreeRADIUS` validates the device credential against PostgreSQL-backed policy data, including `active`, trusted IP, and `max_devices` checks.
6. If a known device attempts to connect from a new IP, `FreeRADIUS` rejects the attempt and forwards an auth event into the portal, which creates an IP confirmation request and sends email.
7. `FreeRADIUS` accounting hooks invoke the canonical host-side forwarder, which posts `Start`, `Interim-Update`, and `Stop` events into the internal API to update `vpn_sessions`.

## Current Operational Contract

- Host env file: `/etc/vpnportal/vpn-host.env`
- Internal endpoint: `POST /api/internal/radius/accounting-events`
- Internal auth endpoint: `POST /api/internal/radius/auth-events`
- Auth header: `X-Internal-Api-Key: <InternalApi:SharedSecret>`
- Canonical host-side helper: `/usr/local/lib/vpnportal/forward-accounting-event.sh`
- Canonical host-side auth helper: `/usr/local/lib/vpnportal/forward-auth-event.sh`
- Canonical FreeRADIUS module file: `/etc/freeradius/3.0/mods-available/exec-accounting`
- Canonical FreeRADIUS auth module file: `/etc/freeradius/3.0/mods-available/exec-auth`
