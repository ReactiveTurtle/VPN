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
- `src/VpnPortal.Application`: use-case services, service interfaces, and DTO contracts
- `src/VpnPortal.Domain`: domain entities, enums, and core state transitions
- `src/VpnPortal.Infrastructure`: EF Core persistence, hashing, token protection, email, runtime adapters, and operational integrations
- `src/VpnPortal.Migrations`: standalone schema migration program
- `src/VpnPortal.Web`: Angular application for public, user, and admin flows

## Major Runtime Flows

### Request and activation

1. Public request is submitted through API.
2. Request is stored in PostgreSQL through the EF Core persistence layer.
3. Superadmin approves request.
4. User record is created if needed.
5. Activation token is generated and emailed.
6. User sets password and activates the account.

### User portal

1. User authenticates with cookie-based auth.
2. Dashboard loads trusted devices, trusted IPs, pending confirmations, and sessions.
3. User can issue or rotate a per-device VPN credential, revoke a device, and request or confirm a trusted IP change.

### Admin operations

1. Superadmin authenticates separately.
2. Admin screens expose requests, users, sessions, and audit data.
3. Superadmin can change `max_devices`, activate or deactivate users, and disconnect sessions.

## Target Deployment Topology

The current target operational topology is a single `Ubuntu 24.04` host that runs:

- `strongSwan`
- `FreeRADIUS`
- `PostgreSQL`
- `VpnPortal.Api`
- static Angular SPA assets
- `nginx`

Bootstrap assets for that host live under `infrastructure/vpn-host/`.

## Target Credential Model

The current codebase stores one password per user for portal authentication.

The target VPN access model is different:

- portal authentication keeps one `Argon2id` password per user
- VPN access uses separate issued credentials per device
- device lifecycle is modeled by issued VPN credentials, not by inferred hardware identity alone

## Integration Boundary

This repository models the data and portal workflows required for strongSwan and FreeRADIUS integration, but full runtime enforcement on the VPN side still depends on:

- server bootstrap and configuration under `infrastructure/vpn-host/`
- final `FreeRADIUS` SQL policy
- runtime use of per-device VPN credentials by the VPN and AAA path

## Schema Management

PostgreSQL schema changes are managed only through `EF Core` migrations under `src/VpnPortal.Migrations/Migrations/`.

The API does not create or migrate the schema on startup.
