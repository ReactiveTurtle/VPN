# Product Overview

## Goal

Build a VPN platform with centralized user onboarding, approval, authentication, and session oversight.

## Roles

- User: submits a VPN access request, activates the account, signs in, manages devices and trusted IPs, and reviews VPN activity.
- Superadmin: approves or rejects requests, manages users, reviews sessions, disconnects sessions, and changes device limits.

## Primary Workflow

1. User submits a request through the portal.
2. Superadmin approves the request.
3. The system generates a time-limited activation link.
4. User sets a password and activates the account.
5. User signs in to the portal and manages VPN-related trust state.
6. VPN access is enforced through strongSwan, FreeRADIUS, and PostgreSQL-backed policy data.

## Scope Notes

- The repository already implements core portal workflows and supporting persistence.
- Some operational VPN-side flows are still documented target state rather than fully wired end-to-end in this repository.
