# VPN Portal OpenCode Context

## Purpose

This repository contains a VPN access portal and supporting infrastructure.

- Backend: `.NET 10 ASP.NET Core API`
- Frontend: `Angular SPA`
- Database: `PostgreSQL`
- VPN: `strongSwan IKEv2`
- AAA: `FreeRADIUS`

## Documentation Policy

- `docs/` is the source of truth for project documentation, architecture notes, and ADRs.
- `AGENTS.md` is the short operational context for OpenCode.
- `.opencode/agents/` contains specialized OpenCode subagents for documentation workflows.
- Do not duplicate large sections of `docs/` inside agent prompts. Reference the relevant document instead.

## Current Implementation State

- The current frontend in the repository is `Angular`, not `Blazor`.
- The backend already includes request submission, account activation, auth, audit, user/admin operations, device/IP trust records, and session views.
- `database/001_schema.sql` is more complete than the original simplified schema from the product brief.
- The schema now includes `vpn_device_credentials` for password-based per-device VPN access.
- VPN device credentials store both an application-side password hash and a `radius_nt_hash` for `MSCHAPv2`/`FreeRADIUS` validation.
- The API now exposes `POST /api/internal/radius/accounting-events` protected by `InternalApi:SharedSecret` for VPN-side session accounting intake.
- `infrastructure/vpn-host/freeradius/sites-available/default.template` is the current source of truth for FreeRADIUS policy checks, including `active` and `max_devices` gating.
- `infrastructure/vpn-host/freeradius/mods-available/exec-accounting.template` is the current source of truth for how FreeRADIUS forwards accounting events into the internal API.
- `infrastructure/vpn-host/freeradius/scripts/forward-accounting-event.sh.template` is the canonical host-side helper for forwarding accounting events into the internal API.
- The current `max_devices` gate is device-aware and counts other active device sessions, so reconnecting the same device should not consume an additional slot by itself.
- `vpn_sessions.session_id` is treated as the stable accounting key for runtime session updates and is unique when present.

## Key Paths

- `src/VpnPortal.Api/` - API host and controllers
- `src/VpnPortal.Application/` - contracts and interfaces
- `src/VpnPortal.Domain/` - domain entities and enums
- `src/VpnPortal.Infrastructure/` - repositories, security, services
- `src/VpnPortal.Web/` - Angular frontend
- `database/` - schema and seed data
- `deploy/` - deployment assets
- `docs/` - project documentation and ADRs
- `infrastructure/vpn-host/` - VPN host bootstrap, FreeRADIUS templates, and server-side operational assets

## Working Rules

- Prefer small, targeted changes.
- When architecture or behavior changes, update `docs/` first or in the same change.
- Reflect actual code state, not only target-state intentions from the original brief.
- Treat `docs/architecture.md`, `docs/database.md`, and `docs/roadmap.md` as the primary documentation touchpoints.
- After each completed implementation cycle, create a separate git commit before starting the next cycle.
