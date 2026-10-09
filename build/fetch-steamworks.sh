#!/usr/bin/env bash
#
# Fetch Facepunch.Steamworks into External/, at the commit pinned in build/steamworks.lock.
# Fade.MonoGame.Steam references it BY SOURCE, and it is not in git, so nothing that
# references Fade.MonoGame.Steam builds until this has run. It also carries the native Steam
# libraries for macOS and Linux, at the version its interop layer was generated for.
#
#   ./build/fetch-steamworks.sh
#
# Safe to run again: it moves an existing checkout to the pinned commit. To move to a newer
# Facepunch.Steamworks, change the commit in build/steamworks.lock and run this again.
#
# Windows twin: fetch-steamworks.ps1 -- keep them in step.
set -euo pipefail
cd "$(dirname "$0")/.."

REPO="https://github.com/Facepunch/Facepunch.Steamworks.git"
DIR="External/Facepunch.Steamworks"
SHA="$(tr -d '[:space:]' < build/steamworks.lock)"

if [ ! -d "$DIR/.git" ]; then
	mkdir -p External
	git clone --quiet "$REPO" "$DIR"
fi
if ! git -C "$DIR" cat-file -e "$SHA^{commit}" 2>/dev/null; then
	git -C "$DIR" fetch --quiet origin
fi
git -C "$DIR" checkout --quiet "$SHA"
echo "Facepunch.Steamworks is at $SHA"
