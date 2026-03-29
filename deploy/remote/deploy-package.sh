#!/usr/bin/env bash
set -euo pipefail

PACKAGE_PATH="${1:?package path is required}"
DEPLOY_TARGET="${2:-production}"
PACKAGE_VERSION="${3:-manual}"

case "$DEPLOY_TARGET" in
  production)
    APP_ROOT="/opt/vpnportal"
    SERVICE_NAME="vpnportal-api"
    ;;
  staging)
    APP_ROOT="/opt/vpnportal-staging"
    SERVICE_NAME="vpnportal-api-staging"
    ;;
  *)
    echo "Unsupported deploy target: $DEPLOY_TARGET" >&2
    exit 1
    ;;
esac

RELEASE_ROOT="$APP_ROOT/releases"
TIMESTAMP="$(date +%Y%m%d%H%M%S)"
TARGET_DIR="$RELEASE_ROOT/${TIMESTAMP}-${PACKAGE_VERSION}"
TOOLS_SOURCE_DIR="$TARGET_DIR/infrastructure/vpn-host/tools"
TOOLS_TARGET_DIR="/usr/local/bin"

mkdir -p "$TARGET_DIR"
tar -xzf "$PACKAGE_PATH" -C "$TARGET_DIR"

ln -sfn "$TARGET_DIR" "$APP_ROOT/current"

if [[ -d "$TOOLS_SOURCE_DIR" ]]; then
  while IFS= read -r -d '' tool_path; do
    sudo install -m 0755 "$tool_path" "$TOOLS_TARGET_DIR/$(basename "$tool_path")"
  done < <(find "$TOOLS_SOURCE_DIR" -maxdepth 1 -type f -print0)
fi

sudo systemctl daemon-reload
sudo systemctl restart "$SERVICE_NAME"
sudo systemctl reload nginx
