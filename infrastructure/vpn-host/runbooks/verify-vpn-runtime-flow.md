# Verify VPN Runtime Flow

Use this runbook after host bootstrap to validate the current password-based VPN flow end-to-end on the server.

## Scope

This verifies the current runtime contract:

- portal user activation and login
- per-device VPN credential issuance
- `FreeRADIUS` credential validation
- `active` and `max_devices` gating
- internal accounting forwarding into `vpn_sessions`
- blocked new-IP forwarding into the portal confirmation flow

## Preconditions

- `Ubuntu 24.04` host bootstrapped with `infrastructure/vpn-host/bootstrap/*.sh`
- portal reachable at `INTERNAL_API_BASE_URL`
- `strongSwan`, `FreeRADIUS`, `PostgreSQL`, and `nginx` running
- at least one activated user with an issued VPN device credential

## Readiness Checks

1. Run `sudo ./infrastructure/vpn-host/bootstrap/07-verify-stack.sh /etc/vpnportal/vpn-host.env`
2. Run `sudo ./infrastructure/vpn-host/bootstrap/08-smoke-test-portal.sh /etc/vpnportal/vpn-host.env`
3. Run `sudo freeradius -CX`
4. Confirm the helper files exist:
   - `/usr/local/lib/vpnportal/forward-accounting-event.sh`
   - `/usr/local/lib/vpnportal/forward-auth-event.sh`

## Happy Path

1. Sign in to the portal as a user.
2. Issue a VPN credential for one device.
3. Follow the platform onboarding guide shown in the portal.
4. Connect once from a client.
5. Verify the session appears in the user dashboard and admin session view.

Expected result:

- VPN tunnel comes up
- `vpn_sessions` contains a row with a non-null `session_id`
- `authorized = true`
- `active = true`

## Accounting Path

1. Connect with an issued device credential.
2. Observe `freeradius` logs during `Start` and `Interim-Update` events.
3. Disconnect the client.
4. Verify a `Stop` event was forwarded.

Expected result:

- `POST /api/internal/radius/accounting-events` is called by the host helper
- `vpn_sessions.last_seen_at` updates on interim traffic
- `vpn_sessions.active` becomes `false` after disconnect

## Device Limit Gate

1. Set `max_devices = 1` for a test user.
2. Issue credentials for two devices.
3. Connect device A successfully.
4. Attempt to connect device B while device A stays active.

Expected result:

- device B is rejected
- `Reply-Message` indicates device limit reached
- reconnecting device A does not fail just because it is the same device

## New IP Confirmation Gate

1. Connect successfully from an already approved IP.
2. Disconnect.
3. Move the same device credential to a different source IP.
4. Attempt to connect again.

Expected result:

- `FreeRADIUS` rejects the connection
- `/usr/local/lib/vpnportal/forward-auth-event.sh` posts to `POST /api/internal/radius/auth-events`
- the portal creates a pending `ip_change_confirmation`
- the user receives an email confirmation link
- after confirmation, the same device can connect from the new IP

## What To Inspect When It Fails

1. `journalctl -u freeradius -n 200 --no-pager`
2. `journalctl -u strongswan-starter -n 200 --no-pager`
3. `journalctl -u nginx -n 100 --no-pager`
4. portal logs for `VpnAccountingService` and `VpnAuthEventService`
5. `select * from vpn_sessions order by started_at desc limit 20;`
6. `select * from ip_change_confirmations order by created_at desc limit 20;`

## Current Limits

- this runbook validates the current manual onboarding approach
- it does not cover `.mobileconfig`, QR, or managed client artifacts
- it does not prove real-time VPN teardown from admin disconnect yet
