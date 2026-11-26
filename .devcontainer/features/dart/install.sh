#!/usr/bin/env bash
# Installs the Dart SDK from the official archive with checksum verification.
# Why a local feature: the devcontainers GitHub organization ships no Dart feature, and the community one
# installs through asdf via nanolayer, a nested installer that pins its own dependencies.
set -euo pipefail

REQUESTED="${VERSION:-latest}"
VERSION="$REQUESTED"
INSTALL_DIR=/usr/lib/dart
BASE_URL=https://storage.googleapis.com/dart-archive/channels/stable/release

case "$(dpkg --print-architecture)" in
    amd64) ARCH=x64 ;;
    arm64) ARCH=arm64 ;;
    *) echo "(!) Unsupported architecture: $(dpkg --print-architecture)" >&2; exit 1 ;;
esac

missing=()
for cmd in curl unzip sha256sum; do
    command -v "$cmd" > /dev/null || missing+=("$cmd")
done
if [ ${#missing[@]} -gt 0 ]; then
    apt-get update -y
    DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends curl unzip ca-certificates coreutils
    rm -rf /var/lib/apt/lists/*
fi

if [ "$VERSION" = "latest" ]; then
    VERSION="$(curl -fsSL --retry 5 "$BASE_URL/latest/VERSION" | sed -n 's/.*"version": *"\([^"]*\)".*/\1/p')"
elif [[ "$VERSION" =~ ^[0-9]+(\.[0-9]+)?$ ]]; then
    # major or major.minor: the newest stable release within it.
    VERSION="$(curl -fsSL --retry 5 "https://storage.googleapis.com/storage/v1/b/dart-archive/o?prefix=channels/stable/release/${VERSION}.&delimiter=/" \
        | grep -oE "release/${VERSION//./\\.}(\.[0-9]+)+/" | sed 's#release/##; s#/##' | sort -V | tail -n 1 || true)"
fi
[ -n "$VERSION" ] || { echo "(!) Could not resolve a stable Dart version for '$REQUESTED'" >&2; exit 1; }

ZIP="dartsdk-linux-${ARCH}-release.zip"
URL="$BASE_URL/$VERSION/sdk/$ZIP"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "Installing Dart SDK $VERSION ($ARCH) from $URL"
curl -fsSL --retry 5 -o "$TMP/$ZIP" "$URL"
curl -fsSL --retry 5 -o "$TMP/$ZIP.sha256sum" "$URL.sha256sum"
(cd "$TMP" && sed 's/ \*/  /' "$ZIP.sha256sum" | sha256sum --check --strict)

rm -rf "$INSTALL_DIR"
unzip -q "$TMP/$ZIP" -d "$TMP"
mv "$TMP/dart-sdk" "$INSTALL_DIR"
chmod -R a+rX "$INSTALL_DIR"
# containerEnv adds $INSTALL_DIR/bin to PATH, but login shells (and `devcontainer exec`) get PATH from
# /etc/profile.d/00-restore-env.sh, which other features snapshot at their own install time.
# /usr/local/bin is on PATH in every case.
ln -sf "$INSTALL_DIR/bin/dart" /usr/local/bin/dart

"$INSTALL_DIR/bin/dart" --version
# Analytics consent is stored per user; opt the container user out so the first dart command does not prompt.
if [ -n "${_REMOTE_USER:-}" ] && id "$_REMOTE_USER" > /dev/null 2>&1; then
    su "$_REMOTE_USER" -c "$INSTALL_DIR/bin/dart --disable-analytics" > /dev/null 2>&1 || true
fi
