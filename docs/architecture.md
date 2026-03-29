# Architecture

## Current Stack

- Backend: `.NET 10 ASP.NET Core Web API`
- Frontend: `Angular SPA`
- Database: `PostgreSQL`
- VPN: `strongSwan IKEv2`
- AAA: `FreeRADIUS`
- Email: SMTP or console email fallback

## Important Clarification

The original brief allowed `Blazor/ASP.NET Core`, but the current repository implements an `Angular` frontend under `src/VpnPortal.Web`.

## Repository Architecture

- `src/VpnPortal.Api`: composition root, authentication setup, controllers, CORS, antiforgery, static asset hosting
- `src/VpnPortal.Application`: service interfaces and DTO contracts
- `src/VpnPortal.Domain`: domain entities and enums
- `src/VpnPortal.Infrastructure`: persistence, hashing, token protection, email, workflow services
- `src/VpnPortal.Web`: Angular application for public, user, and admin flows

## Major Runtime Flows

### Request and activation

1. Public request is submitted through API.
2. Request is stored in PostgreSQL or in-memory storage.
3. Superadmin approves request.
4. User record is created if needed.
5. Activation token is generated and emailed.
6. User sets password and activates the account.

### User portal

1. User authenticates with cookie-based auth.
2. Dashboard loads trusted devices, trusted IPs, pending confirmations, and sessions.
3. User can revoke a device and request or confirm a trusted IP change.

### Admin operations

1. Superadmin authenticates separately.
2. Admin screens expose requests, users, sessions, and audit data.
3. Superadmin can change `max_devices`, activate or deactivate users, and disconnect sessions.

## Integration Boundary

This repository models the data and portal workflows required for strongSwan and FreeRADIUS integration, but full runtime enforcement on the VPN side depends on external server configuration and accounting/auth wiring.
