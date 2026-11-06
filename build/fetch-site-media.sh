#!/usr/bin/env bash
#
# Restore the website's media (site/public/media, site/public/fonts, site/src/lib/media.json), ready to
# build with, from R2, at the bundle site/media.lock pins. The site workflow runs it; the bundle is
# published by build/push-site-media.ps1, which says what is in it.
#
#     ./build/fetch-site-media.sh
#
# Auth as build/assets-config.sh says: CLOUDFLARE_API_TOKEN and CLOUDFLARE_ACCOUNT_ID on CI, the
# wrangler login locally.
set -euo pipefail
cd "$(dirname "$0")/.."
. build/assets-config.sh

LOCK_FILE="site/media.lock"
[ -f "$LOCK_FILE" ] || { echo "fetch-site-media: no $LOCK_FILE. Run build/push-site-media.ps1 and commit it."; exit 1; }
SHA=$(sed -n 's/.*"sha256": *"\([^"]*\)".*/\1/p' "$LOCK_FILE")
VER=$(sed -n 's/.*"version": *\([0-9]*\).*/\1/p' "$LOCK_FILE")
KEY=$(sed -n 's/.*"key": *"\([^"]*\)".*/\1/p' "$LOCK_FILE")

TMP=$(mktemp -d); trap 'rm -rf "$TMP"' EXIT
echo "fetch-site-media: v$VER $SHA"
$WRANGLER r2 object get "$R2_BUCKET/$KEY" --file "$TMP/site-media.zip" --remote

if command -v sha256sum >/dev/null; then GOT=$(sha256sum "$TMP/site-media.zip" | cut -d' ' -f1)
else GOT=$(shasum -a 256 "$TMP/site-media.zip" | cut -d' ' -f1); fi
if [ "$GOT" != "$SHA" ]; then
    echo "fetch-site-media: HASH MISMATCH (wanted $SHA, got $GOT). Refusing to unpack."
    exit 1
fi

unzip -qo "$TMP/site-media.zip" -d .
echo "fetch-site-media: restored $(unzip -Z1 "$TMP/site-media.zip" | grep -vc '/$') files"
