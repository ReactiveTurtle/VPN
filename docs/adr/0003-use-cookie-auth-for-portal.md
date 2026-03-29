# ADR 0003: Use Cookie Authentication for Portal Sessions

## Context

The portal serves browser-based user and admin flows and needs session handling with CSRF protection.

## Decision

Use cookie-based authentication for portal sessions and antiforgery protection for controller actions.

## Consequences

- Browser session handling is straightforward for the SPA.
- CSRF protection remains relevant and must stay configured correctly.
- API consumers outside the browser are not the primary target of this auth model.
