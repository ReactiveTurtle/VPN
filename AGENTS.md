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

## Key Paths

- `src/VpnPortal.Api/` - API host and controllers
- `src/VpnPortal.Application/` - contracts and interfaces
- `src/VpnPortal.Domain/` - domain entities and enums
- `src/VpnPortal.Infrastructure/` - repositories, security, services
- `src/VpnPortal.Web/` - Angular frontend
- `database/` - schema and seed data
- `deploy/` - deployment assets
- `docs/` - project documentation and ADRs

## Working Rules

- Prefer small, targeted changes.
- When architecture or behavior changes, update `docs/` first or in the same change.
- Reflect actual code state, not only target-state intentions from the original brief.
- Treat `docs/architecture.md`, `docs/database.md`, and `docs/roadmap.md` as the primary documentation touchpoints.
- After each completed implementation cycle, create a separate git commit before starting the next cycle.
