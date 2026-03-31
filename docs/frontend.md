# Frontend

## Current Implementation

The frontend is an `Angular SPA` located in `src/VpnPortal.Web`.

## Current Pages

- public landing/request page
- account activation page
- user login page
- admin login page
- user dashboard
- superadmin pending requests page
- superadmin request history page
- superadmin audit page
- superadmin sessions page
- superadmin accounts page

## Current User Experience

- Users can submit requests and activate accounts.
- The user-facing area is labeled `Личный кабинет`, keeps the global header minimal, and focuses the main screen on device access rather than account identity markers.
- Users create per-device VPN access, review only active device credentials in the dashboard, see the IP currently bound to each device, unbind it when needed, and review recent VPN sessions.
- Manual setup help is shown as a secondary tab alongside devices and recent connection history.
- Superadmins work inside a single `Суперадминка` cabinet screen that mirrors the user dashboard pattern: hero summary, compact section tabs, and in-place actions for each workflow.

## Current Limitations

- Platform-specific config delivery for iOS/Android/Windows/macOS is still manual help only; generated config artifacts are not yet available.
- The superadmin UI now follows the same cabinet-style navigation pattern as the user dashboard, but it still relies on the same existing backend endpoints and does not yet provide deeper filtering or search.
