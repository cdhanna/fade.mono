#!/usr/bin/env bash
#
# Restore the bucket-managed asset folders (see build/assets-config.sh) from R2.
#
#     ./build/fetch-assets.sh             # the version this commit is pinned to  <- CI uses this
#     ./build/fetch-assets.sh --latest    # newest published, and rewrite the lock
#     ./build/fetch-assets.sh --clean     # also delete local files that are not in the bundle
#
# The default is the PINNED version, deliberately: these files are build inputs -- the content
# pipeline compiles each one into an .xnb -- so the bytes that go in determine the game that
# comes out. Fetching "latest" during a build would mean an old commit no longer rebuilds the
# same game.
#
# Without --clean, local files the bundle does not have are LEFT ALONE and listed, because they
# are most likely new art that has not been pushed yet. Files the bundle does have are
# overwritten.
set -euo pipefail
cd "$(dirname "$0")/.."
. build/assets-config.sh

LATEST=0; CLEAN=0
for arg in "$@"; do
    case "$arg" in
        --latest) LATEST=1 ;;
        --clean)  CLEAN=1 ;;
        *) echo "fetch-assets: unknown argument $arg"; exit 2 ;;
    esac
done

TMP=$(mktemp -d); trap 'rm -rf "$TMP"' EXIT

if [ "$LATEST" = "1" ]; then
    $WRANGLER r2 object get "$R2_BUCKET/$LATEST_KEY" --file "$TMP/latest.json" --remote
    SHA=$(sed -n 's/.*"sha256": *"\([^"]*\)".*/\1/p' "$TMP/latest.json")
    VER=$(sed -n 's/.*"version": *\([0-9]*\).*/\1/p' "$TMP/latest.json")
    KEY="assets/$SHA.zip"
else
    SHA=$(sed -n 's/.*"sha256": *"\([^"]*\)".*/\1/p' "$LOCK")
    VER=$(sed -n 's/.*"version": *\([0-9]*\).*/\1/p' "$LOCK")
    KEY=$(sed -n 's/.*"key": *"\([^"]*\)".*/\1/p' "$LOCK")
fi
if [ -z "$SHA" ]; then
    echo "fetch-assets: $LOCK pins no bundle. Nothing has been published yet:"
    echo "  run build/push-assets on a machine that has the assets, and commit $LOCK."
    exit 1
fi

echo "fetch-assets: v$VER $SHA"
$WRANGLER r2 object get "$R2_BUCKET/$KEY" --file "$TMP/assets.zip" --remote

if command -v sha256sum >/dev/null; then GOT=$(sha256sum "$TMP/assets.zip" | cut -d' ' -f1)
else GOT=$(shasum -a 256 "$TMP/assets.zip" | cut -d' ' -f1); fi
if [ "$GOT" != "$SHA" ]; then
    echo "fetch-assets: HASH MISMATCH"
    echo "  wanted $SHA"
    echo "  got    $GOT"
    echo "  Refusing to unpack. The bundle was replaced, or the download is corrupt."
    exit 1
fi

if [ "$CLEAN" = "1" ]; then
    for d in $BUCKET_DIRS; do rm -rf "$ASSET_ROOT/$d"; done
fi

mkdir -p "$ASSET_ROOT"
unzip -qo "$TMP/assets.zip" -d "$ASSET_ROOT"
echo "fetch-assets: restored $(unzip -Z1 "$TMP/assets.zip" | grep -vc '/$') files to $ASSET_ROOT"

# Anything local that the bundle does not have. Left in place; see the header.
unzip -Z1 "$TMP/assets.zip" | LC_ALL=C sort > "$TMP/in-bundle"
( cd "$ASSET_ROOT" && for d in $BUCKET_DIRS; do [ -d "$d" ] && find "$d" -type f; done ) \
    | LC_ALL=C sort > "$TMP/local"
EXTRA=$(LC_ALL=C comm -13 "$TMP/in-bundle" "$TMP/local" || true)
if [ -n "$EXTRA" ]; then
    echo "fetch-assets: these local files are NOT in the bundle, and were left alone:"
    echo "$EXTRA" | sed 's/^/    /'
    echo "  New art? Run build/push-assets. Stale? Re-run with --clean."
fi

if [ "$LATEST" = "1" ]; then
    cat > "$LOCK" <<JSON
{
  "_comment": "Which asset bundle this commit builds against. Written by build/push-assets, read by build/fetch-assets. The bundle is named by its own sha256, so a commit names its assets exactly and identical bundles dedupe. An empty sha256 means nothing has been published yet.",
  "version": $VER,
  "sha256": "$SHA",
  "key": "$KEY"
}
JSON
    echo "fetch-assets: lock updated to v$VER -- commit it to move the build"
fi
