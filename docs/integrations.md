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
- The VPN host bootstrap now installs `/usr/local/lib/vpnportal/forward-accounting-event.sh` as the canonical helper for forwarding accounting events to the internal API.
- End-to-end production validation of the AAA path is still pending.

## Target Runtime Flow

1. User requests and activates access through the portal.
2. User signs in to the portal with the portal password.
3. User receives or rotates a VPN device credential for a specific device.
4. `strongSwan` accepts the IKEv2 connection and delegates AAA to `FreeRADIUS`.
5. `FreeRADIUS` validates the device credential against PostgreSQL-backed policy data, including `active` state and `max_devices` checks.
6. VPN-side accounting events can be forwarded to the portal internal endpoint to update `vpn_sessions` for portal visibility.

## Current Operational Contract

- Host env file: `/etc/vpnportal/vpn-host.env`
- Internal endpoint: `POST /api/internal/radius/accounting-events`
- Auth header: `X-Internal-Api-Key: <InternalApi:SharedSecret>`
- Canonical host-side helper: `/usr/local/lib/vpnportal/forward-accounting-event.sh`
