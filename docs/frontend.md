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
- The user-facing area is labeled `Личный кабинет` and uses the signed-in email as the primary identity marker in the toolbar and page header.
- Users create per-device VPN access, review the login/password lifecycle for each device, see the IP currently bound to that device, unbind it when needed, and review VPN sessions.
- Manual setup help is shown near the device-access workflow instead of being buried at the bottom of the page.
- Superadmins can switch between separate operational pages for pending requests, request history, audit, sessions, and account management.

## Current Limitations

- Platform-specific config delivery for iOS/Android/Windows/macOS is still manual help only; generated config artifacts are not yet available.
- The superadmin UI is operational and separated by workflow, but it still relies on the same existing backend endpoints and does not yet provide deeper filtering or search.
