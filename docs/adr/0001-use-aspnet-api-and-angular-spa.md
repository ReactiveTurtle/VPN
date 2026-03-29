# ADR 0001: Use ASP.NET Core API and Angular SPA

## Context

The project brief allowed multiple frontend options, but the repository implementation needed a concrete web stack.

## Decision

Use `ASP.NET Core Web API` for backend services and `Angular SPA` for the web frontend.

## Consequences

- Frontend and backend are separated logically.
- Static frontend assets can be served by the API host in deployment.
- Documentation must reflect Angular as the actual frontend implementation.
