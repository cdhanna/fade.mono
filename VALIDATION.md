# Validating a FadeBasic.MonoGame release locally

End-to-end recipe to build a fresh Fade **core** version, build the **MonoGame**
packages against it, and prove the whole consumer story (desktop game, command
plugin, web export) on your machine before publishing to NuGet.

It mirrors what the `Release` GitHub Action does, but pushes to the local
`LocalFade` feed instead of nuget.org, so nothing leaves your machine.

> Worked example below uses **core `0.0.2.595`** and **MonoGame `0.1.0.8`** —
> the versions this validation pass was last run with. Bump both when you
> repeat it (see *Versioning notes*).

## Prerequisites

- .NET SDK 8.0.x (`dotnet --version`).
- The `LocalFade` feed registered once (the core repo's `setup.sh` does this):
  ```sh
  cd <core-repo>/FadeBasic && bash ./setup.sh
  dotnet nuget list source        # should list "LocalFade"
  ```
- For the **web** step only: the Blazor WASM workload —
  `dotnet workload install wasm-tools`.

Paths below assume the core repo (`dby`) and this repo are siblings:
`…/dby/FadeBasic` and `…/Fade.MonoGame/Fade.MonoGame`.

---

## 1. Build a fresh Fade core → LocalFade

```sh
cd <core-repo>/FadeBasic
FADE_USE_LOCAL_SOURCE=true bash ./install.sh 0.0.2 595 LocalFade
#                                              ^^^^^ ^^^  VERSION  BUILD  → core 0.0.2.595
```

This builds `build.sln` and pushes every core package — including the five the
MonoGame side depends on: `FadeBasic.Lang.Core`, `FadeBasic.Lib.Standard`,
`FadeBasic.Lang.CommandSourceGenerator`, `FadeBasic.Testing`, `FadeBasic.Build`.

Verify they landed:
```sh
ls <core-repo>/FadeBasic/obj/LocalFade/FadeBasic.Lang.Core.0.0.2.595.nupkg
```

> **Grab this version** — `0.0.2.595` — it's what you pass to the MonoGame build.

---

## 2. Build the MonoGame packages against that core version → LocalFade

```sh
cd <this-repo>            # …/Fade.MonoGame/Fade.MonoGame  (where install.sh lives)
FADE_USE_LOCAL_SOURCE=true ./install.sh 0.1.0 8 LocalFade "" 0.0.2.595
#                                        ^^^^^ ^ ^^^^^^^^^ ^  ^^^^^^^^^
#                                        VER  BUILD SOURCE  KEY  FADE_VERSION
```

The 5th argument is the **Fade core version** these packages depend on (you can
also use `FADE_VERSION=0.0.2.595` or `-p:FadeVersion=0.0.2.595`). It flows to
every `FadeBasic.*` PackageReference *and* the generator bundled into
`FadeBasic.MonoGame.Game`, so the build-time compiler and the runtime VM match.

Produces (all at `0.1.0.8`): `FadeBasic.MonoGame.Contracts`, `…Game`, `…Lib`,
`…Templates`, and `FadeBasic.Export.MonoGame` (the WASM bundle). Confirm the
dependency pin:
```sh
cd <core-repo>/FadeBasic/obj/LocalFade
unzip -p FadeBasic.MonoGame.Game.0.1.0.8.nupkg '*.nuspec' | grep 'FadeBasic.Lang.Core'
#   → <dependency id="FadeBasic.Lang.Core" version="0.0.2.595" …/>
```

> Add `--skip-wasm` to skip the (slower) WASM bundle when you only need the
> desktop packages.

---

## 3. Validate the desktop game (package consumer)

```sh
dotnet new uninstall FadeBasic.MonoGame.Templates 2>/dev/null
dotnet new install <core-repo>/FadeBasic/obj/LocalFade/FadeBasic.MonoGame.Templates.0.1.0.8.nupkg

dotnet new fadebasic-monogame -o /tmp/val-game -n ValGame
cat > /tmp/val-game/game.fbasic <<'EOF'
t = 1
do
  print "hello world", t
  t = t + 1
  sync
loop
EOF

cd /tmp/val-game
dotnet run -c Release
```

**Expect:** a MonoGame window opens; the console prints `hello world` with an
incrementing counter; **no** `ContentLoadException` (the engine sprite shader is
baked into `FadeBasic.MonoGame.Game`). Close the window to exit.

Check the generated `ValGame.csproj` — it references only `FadeBasic.MonoGame.Lib`
plus the platform packages, **no `FadeBasic.*` core version** (the generator
rides in transitively from `.Game`).

## 4. Validate a command plugin (the extensibility path)

```sh
dotnet new fadebasic-monogame-commands -o /tmp/val-cmd -n ValCmd
cd /tmp/val-cmd && dotnet build -c Release
```

**Expect:** `Build succeeded`, and **no** `Launch/GeneratedFade.g.cs` is emitted
(a plugin has no `<FadeSource>`, so the bundled generator correctly no-ops):
```sh
find /tmp/val-cmd -name GeneratedFade.g.cs -not -path '*/obj/*'   # → no output
```

## 5. Validate the web / KNI sprite effect

The Playground's monogame runtime is built from `WebRuntime.MonoGame` (which
references `.Game` by source), so it exercises the Web bake + the KNI-patched
(MGFX v10) `FadeSpriteBatchEffect`.

```sh
cd <core-repo>/Playground
node scripts/build-monogame-runtime.mjs     # publishes WebRuntime.MonoGame (Web) → public/runtime/monogame/
npm run dev                                 # in one terminal (https://localhost:5311)
node scripts/probe-mg-sprite-effect.mjs     # in another
```

**Expect:** `── VERDICT ──` with `no effect/MGFX exception : true`, and a white
sprite in the Game panel of `/tmp/fade-mg-sprite.png`. (The pixel-count line can
read 0 — headless WebGL `preserveDrawingBuffer` quirk; the screenshot is the
source of truth.)

---

## Versioning notes

- **Always bump both versions** when you re-run. NuGet caches by exact version,
  so reusing `0.0.2.595` / `0.1.0.8` would resolve the *cached* package, not
  your fresh pack. Pick the next build number each time.
- If a rebuild at the *same* version ever seems stale, clear the cache for it:
  ```sh
  rm -rf ~/.nuget/packages/fadebasic.lang.core/0.0.2.595   # etc. for each package
  ```
- Pick a core version **higher than any already published** (LocalFade, the
  Beamable feed, nuget.org) — otherwise NuGet may resolve a higher one
  transitively and raise `NU1605: package downgrade`.

## Publishing for real

Same scripts, real feed. Locally:
```sh
cd <core-repo>/FadeBasic && bash ./install.sh 0.0.2 595 https://api.nuget.org/v3/index.json $CORE_KEY
cd <this-repo>            && ./install.sh 0.1.0 8 https://api.nuget.org/v3/index.json $MG_KEY 0.0.2.595
```
Or via CI: run the **Release** workflow (Actions tab) — set `fadeVersion` to the
published core version and provide the `NUGET_HOST` / `NUGET_API_KEY` secrets.
Publish the **core** version first; the MonoGame `fadeVersion` must already exist
on the target feed.
