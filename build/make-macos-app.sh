#!/usr/bin/env bash
#
# Assemble a macOS .app bundle from a `dotnet publish` output directory.
#
#   usage: make-macos-app.sh <publish-dir> <output-dir>
#
# Steam's macOS launch option points at a .app, not a bare binary, so the publish output has
# to be wrapped before it can go in the depot.
set -euo pipefail

PUBLISH_DIR="${1:?usage: make-macos-app.sh <publish-dir> <output-dir>}"
OUT_DIR="${2:?usage: make-macos-app.sh <publish-dir> <output-dir>}"

# The bundle is <APP_NAME>.app. This is what the macOS launch option in Steamworks names.
APP_NAME="${APP_NAME:-FishFishPop}"
# What Finder and the Dock show.
DISPLAY_NAME="${DISPLAY_NAME:-Fish Fish Pop}"
# The file `dotnet publish` produced, which is the assembly name.
EXE_NAME="${EXE_NAME:-Fade.MonoGame}"
# SWAP IN (optional): a reverse-DNS id that is yours. Only matters once you sign or notarize.
BUNDLE_ID="${BUNDLE_ID:-com.fishfishpop.game}"
# CFBundleVersion wants a monotonic number; CI passes the run number.
BUILD_NUMBER="${BUILD_NUMBER:-1}"
SHORT_VERSION="${SHORT_VERSION:-1.0.0}"
MIN_MACOS="11.0"

APP="$OUT_DIR/$APP_NAME.app"

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

# Managed dlls and dylibs live alongside the executable in Contents/MacOS.
cp -R "$PUBLISH_DIR"/. "$APP/Contents/MacOS/"
[ -f "$APP/Contents/MacOS/$EXE_NAME" ] \
	|| { echo "make-macos-app.sh: no executable named $EXE_NAME in $PUBLISH_DIR" >&2; exit 1; }
chmod +x "$APP/Contents/MacOS/$EXE_NAME"

# Content, however, goes in Contents/Resources, NOT next to the executable.
#
# Once the game is in a bundle, MonoGame looks for content in Contents/Resources and no longer
# looks next to the executable. A Content folder left in Contents/MacOS is invisible, and
# every texture, sound and font fails to load.
if [ -d "$APP/Contents/MacOS/Content" ]; then
	mv "$APP/Contents/MacOS/Content" "$APP/Contents/Resources/Content"
else
	echo "make-macos-app.sh: no Content/ in $PUBLISH_DIR -- the build produced no assets" >&2
	exit 1
fi

# Debug symbols and the run log of whoever built this have no business in the bundle.
find "$APP/Contents/MacOS" -maxdepth 1 \( -name '*.pdb' -o -name '_lastRun.log' \) -delete

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleName</key>
	<string>$DISPLAY_NAME</string>
	<key>CFBundleDisplayName</key>
	<string>$DISPLAY_NAME</string>
	<key>CFBundleIdentifier</key>
	<string>$BUNDLE_ID</string>
	<key>CFBundleExecutable</key>
	<string>$EXE_NAME</string>
	<key>CFBundlePackageType</key>
	<string>APPL</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundleShortVersionString</key>
	<string>$SHORT_VERSION</string>
	<key>CFBundleVersion</key>
	<string>$BUILD_NUMBER</string>
	<key>LSMinimumSystemVersion</key>
	<string>$MIN_MACOS</string>
	<key>LSApplicationCategoryType</key>
	<string>public.app-category.games</string>
	<!-- Without this the game renders at 1x and looks blurry on Retina. -->
	<key>NSHighResolutionCapable</key>
	<true/>
</dict>
</plist>
PLIST

# NO codesign step, deliberately. Two things get conflated under "signing":
#
#   1. An ad-hoc signature, which arm64 macOS requires to EXECUTE anything at all. Strip it
#      and the kernel SIGKILLs the process with no output. `dotnet publish -r osx-arm64`
#      already provides this, and build/check-macho-signed.py asserts it.
#
#   2. Developer ID signing + notarization, which is what Gatekeeper wants for a DOWNLOADED
#      app. Steam does not quarantine what it installs, so a Steam build runs without it.
#      If you ever hand the .app out directly, you will need it.

echo "built $APP"
