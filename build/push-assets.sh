#!/usr/bin/env bash
#
# Bundle the bucket-managed asset folders (see build/assets-config.sh) into a zip, upload it to
# R2, and pin it in build/assets.lock.
#
#     ./build/push-assets.sh            # publish the current working set
#
# NAMED BY CONTENT HASH, not by git SHA. Keying on the commit would be circular -- recording
# "assets = <sha>" changes the sha -- and would not work for uncommitted art. A content hash also
# means identical bundles dedupe for free.
#
# Needs: node (for npx wrangler), and `npx wrangler login` done once.
set -euo pipefail
cd "$(dirname "$0")/.."
. build/assets-config.sh

# Git Bash on Windows ships no `zip`. The PowerShell twin builds the archive itself, so hand
# over to it rather than fail.
if ! command -v zip >/dev/null && command -v powershell.exe >/dev/null; then
    echo "push-assets: no zip here, running build/push-assets.ps1 instead"
    exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File build/push-assets.ps1
fi
command -v zip >/dev/null || { echo "push-assets: zip not found"; exit 1; }

command -v npx >/dev/null || { echo "push-assets: npx not found (wrangler needs node)"; exit 1; }
for d in $BUCKET_DIRS; do
    [ -d "$ASSET_ROOT/$d" ] || { echo "push-assets: $ASSET_ROOT/$d does not exist"; exit 1; }
done

TMP=$(mktemp -d); trap 'rm -rf "$TMP"' EXIT
ZIP="$TMP/assets.zip"

# -X drops extended attributes. Sorted file order so the same inputs give the same archive.
( cd "$ASSET_ROOT" && find $BUCKET_DIRS -type f ! -name '.DS_Store' ! -name 'Thumbs.db' \
    | LC_ALL=C sort > "$TMP/files" && zip -qX "$ZIP" -@ < "$TMP/files" )

if command -v sha256sum >/dev/null; then SHA=$(sha256sum "$ZIP" | cut -d' ' -f1)
else SHA=$(shasum -a 256 "$ZIP" | cut -d' ' -f1); fi
KEY="assets/$SHA.zip"
SIZE=$(wc -c < "$ZIP" | tr -d ' ')

echo "push-assets: $(wc -l < "$TMP/files" | tr -d ' ') files, $((SIZE/1024)) KB"
echo "             sha256 $SHA"

# NOTHING TO DO if the working set already matches the pin. Without this, re-running bumps the
# version and rewrites the lock for byte-identical assets.
LOCKED=$(sed -n 's/.*"sha256": *"\([^"]*\)".*/\1/p' "$LOCK" 2>/dev/null || true)
LOCKED_VER=$(sed -n 's/.*"version": *\([0-9]*\).*/\1/p' "$LOCK" 2>/dev/null || true)
if [ "$SHA" = "$LOCKED" ]; then
    echo "push-assets: unchanged, already pinned as v$LOCKED_VER. Nothing to do."
    exit 0
fi

VER=$(( ${LOCKED_VER:-0} + 1 ))

$WRANGLER r2 object put "$R2_BUCKET/$KEY" --file "$ZIP" --remote

# Read it back before pinning it. The lock must never name a bundle the bucket does not hold:
# every later build would fail, on a machine that may not have the files to push again.
$WRANGLER r2 object get "$R2_BUCKET/$KEY" --file "$TMP/check.zip" --remote
if command -v sha256sum >/dev/null; then BACK=$(sha256sum "$TMP/check.zip" | cut -d' ' -f1)
else BACK=$(shasum -a 256 "$TMP/check.zip" | cut -d' ' -f1); fi
[ "$BACK" = "$SHA" ] || { echo "push-assets: the bundle did not arrive in $R2_BUCKET intact. Nothing was pinned."; exit 1; }

# The mutable pointer, for `fetch-assets --latest`. Builds never read this -- they read the lock.
echo "{\"version\":$VER,\"sha256\":\"$SHA\",\"key\":\"$KEY\"}" > "$TMP/latest.json"
$WRANGLER r2 object put "$R2_BUCKET/$LATEST_KEY" --file "$TMP/latest.json" --remote

cat > "$LOCK" <<JSON
{
  "_comment": "Which asset bundle this commit builds against. Written by build/push-assets, read by build/fetch-assets. The bundle is named by its own sha256, so a commit names its assets exactly and identical bundles dedupe. An empty sha256 means nothing has been published yet.",
  "version": $VER,
  "sha256": "$SHA",
  "key": "$KEY"
}
JSON
echo "push-assets: published v$VER -- commit build/assets.lock to pin it"
