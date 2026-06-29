#!/bin/bash
# install.sh — build and publish FadeBasic MonoGame NuGet packages.
#
# Lives at the repo root (the one CI checks out). Run it from here.
#
# Usage:
#   ./install.sh [VERSION] [BUILD_NUMBER] [PACKAGE_SOURCE] [API_KEY] [FADE_VERSION] [--skip-wasm]
#
# Defaults:
#   VERSION        0.1.0          (MonoGame track; separate from core FadeBasic)
#   BUILD_NUMBER   1
#   PACKAGE_SOURCE LocalFade      (local dev feed registered by core FadeBasic/setup.sh)
#   API_KEY        (empty)
#   FADE_VERSION   (unset → the pinned value in Directory.Build.props)
#
# Specifying the Fade CORE dependency version:
#   The 5th argument (or the FADE_VERSION env var) sets which FadeBasic core
#   version every produced package depends on — it flows to every FadeBasic.*
#   PackageReference AND the generator bundled into FadeBasic.MonoGame.Game, so
#   the build-time compiler and the runtime VM stay locked together. When
#   omitted, Directory.Build.props' pinned default is used.
#     ./install.sh 0.1.0 7 nuget.org $KEY 0.0.3.0
#     FADE_VERSION=0.0.3.0 ./install.sh 0.1.0 7 nuget.org $KEY
#
# Environment flags (mirrors core FadeBasic/install.sh):
#   FADE_USE_LOCAL_SOURCE  — push to LocalFade instead of PACKAGE_SOURCE
#   FADE_NUGET_DRYRUN      — skip all nuget push steps
#
# Prerequisites:
#   - The Fade CORE NuGet packages at $FADE_VERSION must be resolvable from a
#     configured source (nuget.org in CI; LocalFade for local dev).
#   - The Fade.MonoGame sample project is intentionally excluded from packaging.

set -e

VERSION=${1:-0.1.0}
BUILD_NUMBER=${2:-1}
PACKAGE_SOURCE=${3:-LocalFade}
PACKAGE_SOURCE_API_KEY=${4}
# 5th positional arg overrides FADE_VERSION; otherwise honor the env var.
if [ -n "$5" ] && [ "$5" != "--skip-wasm" ]; then FADE_VERSION=$5; fi

SKIP_WASM=false
for arg in "$@"; do
  case $arg in --skip-wasm) SKIP_WASM=true ;; esac
done

SEM_VER="${VERSION}.${BUILD_NUMBER}"
OUTPUT_FOLDER="bin/artifacts_${SEM_VER}"
NUGET_KEY_STR=${PACKAGE_SOURCE_API_KEY:+"--api-key $PACKAGE_SOURCE_API_KEY"}
# When set, pin the Fade core dependency version across every build/pack/publish.
FADE_VERSION_ARG=${FADE_VERSION:+/p:FadeVersion=$FADE_VERSION}

SOLUTION_DIR="."

echo "cleaning old output folders..."
rm -rf "$OUTPUT_FOLDER"

echo "installing FadeBasic MonoGame packages version=${SEM_VER} (Fade core=${FADE_VERSION:-Directory.Build.props default})"

# Everything targets net8.0 now (KNI requires net8; DesktopGL runs on it too),
# so there's a single TFM. The MonoGame libs reference FadeBasic.* purely via
# PackageReference ($(FadeVersion)), so the packed nupkgs are clean. We build the
# packable LIB projects here (Desktop); the web host is built by its own publish
# step below — it needs FadeMonoGamePlatform=Web, which a single whole-solution
# build can't satisfy alongside the desktop projects (the example exe stays
# Desktop, WebRuntime must be Web).
BUILD_ARGS="-c Release /p:Version=$SEM_VER $FADE_VERSION_ARG"
# Content is published too: games opt into in-process content hot-reload by
# referencing FadeBasic.MonoGame.Content (Debug desktop) — see the template.
for proj in Contracts Game Lib Content; do
  dotnet clean "$SOLUTION_DIR/Fade.MonoGame.$proj" -c Release
  dotnet build "$SOLUTION_DIR/Fade.MonoGame.$proj" $BUILD_ARGS
done

# Pack the library packages. net8.0 is the only TFM, so no -f is needed.
LIB_PACK_ARGS="--output $OUTPUT_FOLDER /p:Version=$SEM_VER $FADE_VERSION_ARG \
  --include-symbols --include-source -p:SymbolPackageFormat=snupkg \
  -c Release"

dotnet pack "$SOLUTION_DIR/Fade.MonoGame.Contracts" $LIB_PACK_ARGS
dotnet pack "$SOLUTION_DIR/Fade.MonoGame.Game"      $LIB_PACK_ARGS
dotnet pack "$SOLUTION_DIR/Fade.MonoGame.Lib"       $LIB_PACK_ARGS
dotnet pack "$SOLUTION_DIR/Fade.MonoGame.Content"   $LIB_PACK_ARGS

# Templates package: the `dotnet new fadebasic-monogame` templates. It's
# content-only and not built, so no symbols. /p:Version stamps the
# FadeBasic.MonoGame.* references in the generated csprojs; $(FadeVersion) +
# the platform versions (from Directory.Build.props) stamp the rest.
dotnet pack "$SOLUTION_DIR/Fade.MonoGame.Templates" \
  --output "$OUTPUT_FOLDER" /p:Version=$SEM_VER $FADE_VERSION_ARG -c Release

# FadeBasic.Export.MonoGame — content-only WASM bundle (no symbols).
#   1. dotnet publish  → produces the wwwroot/ WASM artifacts
#   2. dotnet pack     → bundles them into the NuGet content
if [ "$SKIP_WASM" = false ]; then
  WASM_ARTIFACT_DIR="$PWD/bin/wasm_${SEM_VER}"

  # WebRuntime is the Blazor/KNI web host: it (and its lib graph) must build with
  # FadeMonoGamePlatform=Web. This global flag is the reliable way to flavor the
  # whole graph (an in-project SetProperties/self-pin does NOT propagate it).
  echo "publishing WebRuntime.MonoGame WASM bundle..."
  dotnet publish "$SOLUTION_DIR/WebRuntime.MonoGame" \
    -c Release -f net8.0 -p:FadeMonoGamePlatform=Web $FADE_VERSION_ARG -o "$WASM_ARTIFACT_DIR"

  dotnet pack "$SOLUTION_DIR/WebRuntime.MonoGame" \
    --output "$OUTPUT_FOLDER" \
    /p:Version="$SEM_VER" \
    $FADE_VERSION_ARG \
    /p:FADE_MG_WASM_ARTIFACT_DIR="$WASM_ARTIFACT_DIR" \
    /p:IsPack=true \
    -p:FadeMonoGamePlatform=Web \
    -c Release
else
  echo "skipping WASM build (--skip-wasm)"
fi

# Push packages to NuGet source.
if [ -z "$FADE_USE_LOCAL_SOURCE" ]; then
  if [ -z "$FADE_NUGET_DRYRUN" ]; then
    echo "pushing packages to: ${PACKAGE_SOURCE}"
    # This glob pushes EVERY nupkg in the output folder, including
    # FadeBasic.Export.MonoGame.$SEM_VER (it ends in .$BUILD_NUMBER.nupkg) when
    # WASM was built — so no separate Export.MonoGame push is needed.
    # --skip-duplicate makes re-running a release idempotent.
    dotnet nuget push "$OUTPUT_FOLDER/*.$BUILD_NUMBER.nupkg" \
      --source "$PACKAGE_SOURCE" $NUGET_KEY_STR --skip-duplicate
  else
    echo "skipping NuGet push (FADE_NUGET_DRYRUN is set)"
  fi
else
  echo "pushing to local feed (LocalFade)..."
  dotnet nuget push "$OUTPUT_FOLDER/*.$BUILD_NUMBER.nupkg" --source "LocalFade" --skip-duplicate
fi
