# Deployment

## Current Assets

Deployment assets already exist under `deploy/`.

- `deploy/systemd/`
- `deploy/nginx/`
- `deploy/env/`
- `deploy/remote/`
- `.github/workflows/`

## Shape

- API is intended to run on Linux under `systemd`.
- `nginx` is expected to front the application.
- Frontend build output is served as static files from the API host.
- CI and deploy workflows package and ship the application.

## Reference

See `deploy/README.md` for current deployment notes and server preparation steps.
