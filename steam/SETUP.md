# Fish Fish Pop on Steam: setup guide

Everything you need to do once so that **Actions → Steam (Fish Fish Pop) → Run workflow**
builds the game for Windows and macOS and uploads both to Steam.

The code side is done. What is left is accounts, ids and secrets that only you can create.
Budget about an hour, most of it waiting on Steamworks.

## What the workflow does

`.github/workflows/steam.yml`, on one Linux runner:

1. Downloads the textures, audio and fonts from a Cloudflare R2 bucket (`build/fetch-assets.sh`),
   at the exact version pinned in `build/assets.lock`.
2. Publishes `Fade.MonoGame/Fade.MonoGame.csproj` in Release for `win-x64` and `osx-arm64`,
   self-contained, so players need no .NET install. The Release build compiles `Assets/` into
   `Content/*.xnb` and runs the Fade program that was compiled at build time.
3. Wraps the macOS build in `FishFishPop.app` (`build/make-macos-app.sh`).
4. Uploads both to Steam with `steamcmd` (`steam/*.vdf`) and sets the build live on a beta
   branch.

## Everything you have to swap in

| What | Where it goes | Kind |
|---|---|---|
| Steam app id | GitHub variable `STEAM_APP_ID` | required |
| Windows depot id | GitHub variable `STEAM_DEPOT_WINDOWS` | required |
| macOS depot id | GitHub variable `STEAM_DEPOT_MAC` | required |
| Builder account login name | GitHub secret `STEAM_USERNAME` | required |
| Builder account `config.vdf`, base64 | GitHub secret `STEAM_CONFIG_VDF` | required |
| Cloudflare R2 token (read only) | GitHub secret `CLOUDFLARE_API_TOKEN` | required |
| Cloudflare account id | GitHub secret `CLOUDFLARE_ACCOUNT_ID` | required |
| R2 bucket name | `build/assets-config.sh`, `build/push-assets.ps1`, `build/fetch-assets.ps1` (all three say `fish-game-assets`) | only if you pick another name |
| macOS bundle id | `BUNDLE_ID` in `build/make-macos-app.sh` (`com.fishfishpop.game`) | optional |

Nothing in `steam/*.vdf` needs editing. The ids are filled in from the GitHub variables when
the workflow runs.

In Steamworks, the launch options must name these two files:

| OS | Executable |
|---|---|
| Windows | `Fade.MonoGame.exe` |
| macOS | `FishFishPop.app` |

---

## 1. Cloudflare bucket, and moving the assets out of git

`Fade.MonoGame/Assets/Fish/Audio`, `Fonts` and `Textures` are now ignored by git. They travel
as one zip in an R2 bucket, named by its own sha256, and `build/assets.lock` records which zip
a commit builds against. Shaders stay in git.

> **Until you finish this step, most of those files exist only on your disk.** The music,
> card art, logos and menu art were never committed, and git no longer lists them as
> untracked. Do this step before anything happens to this machine.

1. Create a Cloudflare account and turn on **R2**. It asks for a payment method; the free
   tier (10 GB) is far more than the ~11 MB this needs.
2. Create a bucket named `fish-game-assets`. If you choose another name, change it in the
   three files listed in the table above.
3. Install Node.js, which the scripts use to run Cloudflare's `wrangler`:

   ```powershell
   winget install OpenJS.NodeJS.LTS
   ```

   Open a new terminal afterwards, then log in once:

   ```powershell
   npx wrangler login
   ```

4. Publish the assets and pin them:

   ```powershell
   .\build\push-assets.ps1
   ```

   It prints `published v1` and rewrites `build/assets.lock`.

5. Take the 26 files that are still tracked out of git. They stay on your disk:

   ```powershell
   git rm -r -q --cached Fade.MonoGame/Assets/Fish/Audio Fade.MonoGame/Assets/Fish/Fonts Fade.MonoGame/Assets/Fish/Textures
   git add build/assets.lock
   ```

   Commit with the rest in step 5 of this guide.

6. Make the read-only token for CI. In the Cloudflare dashboard: **R2 → Manage API tokens →
   Create API token**, permission **Object Read only**, limited to this bucket. Keep the
   **Token value** (not the S3 access key pair). The **Account ID** is on the R2 overview
   page. Both go into GitHub in step 4.

The old copies stay in git history. That is about 6 MB, so it is not worth rewriting history
for.

### Day to day, after this

| You did | Run |
|---|---|
| Changed, added or deleted art, audio or fonts | `.\build\push-assets.ps1`, then commit `build/assets.lock` |
| Cloned the repo, or pulled a commit with a new lock | `.\build\fetch-assets.ps1` |
| Want to drop local files that are not in the bundle | `.\build\fetch-assets.ps1 -Clean` |

A build fails with a message naming `fetch-assets` if the folders are missing. `fetch-assets`
overwrites files the bundle has, and leaves alone (and lists) local files it does not have,
so unpublished new art is safe. A commit that changes assets but not the lock will build with
the old assets in CI.

On macOS or Linux use the `.sh` twins of both scripts.

---

## 2. Steamworks: the app, two depots, launch options

1. In Steamworks, create the app (this is the step with the Steam Direct fee). Note the
   **App ID**.
2. **SteamPipe → Depots**: you need two depots. A new app usually comes with one already,
   numbered app id + 1. Add a second. Set one to **Windows** and the other to **macOS**.
   Note both ids.
3. **Installation → General Installation → Launch Options**, add two:
   - Executable `Fade.MonoGame.exe`, operating system Windows.
   - Executable `FishFishPop.app`, operating system macOS. The build is Apple Silicon only,
     so if the page offers a CPU architecture for macOS, choose Apple Silicon / ARM64.
4. Check that both depots are in the app's packages (the developer comp package at least),
   or testers on one OS will get nothing to download.
5. **Publish** the Steamworks changes. Unpublished depot and launch settings do nothing.

---

## 3. A Steam builder account, and its login for CI

`steamcmd` cannot answer a Steam Guard prompt inside CI. So you log in once on your own
machine, and CI reuses the saved login.

1. Create a **new** Steam account for builds, with Steam Guard by email. Do not use your own:
   CI would hold a login to everything you own, and logging in elsewhere can end its session.
2. In Steamworks: **Users & Permissions → Add user**, invite that account, accept, and give
   it access to this app only, with **Edit App Metadata** and **Publish App Changes To
   Steam**.
3. Get steamcmd into its own folder and log in. On Windows, steamcmd keeps everything in its
   own folder and does not touch your installed Steam client.

   ```powershell
   mkdir C:\steamcmd-ci; cd C:\steamcmd-ci
   curl.exe -sSLO https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
   Expand-Archive steamcmd.zip .
   .\steamcmd.exe +login YOUR_BUILDER_LOGIN +quit
   ```

   Enter the password, then the Steam Guard code from the email.

4. **Run the same login command a second time.** It must log in without asking for a code.
   If it asks again, the login was not saved and CI will fail the same way.

5. Copy the saved login to the clipboard as base64:

   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\steamcmd-ci\config\config.vdf")) | Set-Clipboard
   ```

   That is the value of `STEAM_CONFIG_VDF`.

The saved login lasts months, not forever. When the workflow starts failing at login, repeat
steps 3 to 5 and replace the secret.

---

## 4. GitHub secrets and variables

**Repo → Settings → Secrets and variables → Actions.**

On the **Secrets** tab:

| Name | Value |
|---|---|
| `STEAM_USERNAME` | the builder account's login name (not its display name or email) |
| `STEAM_CONFIG_VDF` | the base64 from step 3 |
| `CLOUDFLARE_API_TOKEN` | the R2 token value from step 1 |
| `CLOUDFLARE_ACCOUNT_ID` | the Cloudflare account id from step 1 |

On the **Variables** tab:

| Name | Value |
|---|---|
| `STEAM_APP_ID` | the app id |
| `STEAM_DEPOT_WINDOWS` | the Windows depot id |
| `STEAM_DEPOT_MAC` | the macOS depot id |

---

## 5. Commit and push

The workflow builds what is on GitHub, not what is on your disk. Commit and push:

- the new files: `.github/workflows/steam.yml`, `build/`, `steam/`
- the changes to `.gitignore`, `Fade.MonoGame/Fade.MonoGame.csproj`, `Fade.MonoGame/Program.cs`
- `build/assets.lock` and the asset removals from step 1
- your game work, including the shader files under `Assets/Fish/Shaders` that are still
  untracked. A shader missing from git fails in CI with nothing wrong locally.

GitHub only offers **Run workflow** once `steam.yml` is on the default branch.

---

## 6. First run: build only

**Actions → Steam (Fish Fish Pop) → Run workflow**, and untick **deploy**.

This needs only the two Cloudflare secrets. It builds both platforms and attaches them to the
run as the artifact `fishfishpop-builds`. Download it and run both builds before anything
goes to Steam:

- Windows: unzip, run `Fade.MonoGame.exe`.
- macOS: `tar -xzf FishFishPop-macos-arm64.tar.gz`, then, because a downloaded app is
  quarantined and this one is not notarized: `xattr -dr com.apple.quarantine FishFishPop.app`
  and open it. Steam installs do not need that.

`FISH_SKIP_SPLASH=1` and `FISH_SKIP_MENU=1` still work for getting to the board quickly.

**Test the macOS build on a real Mac here.** It has never been launched on one. See "Not
verified" below.

---

## 7. First upload, then the real thing

Steamworks will not let you create a beta branch until the app has a build, so the first
upload sets nothing live.

1. Run the workflow with **deploy** ticked and **set_live** = `none`.
2. In Steamworks, **SteamPipe → Builds** now shows the build. Create a branch named `test`
   (with a password if you want it hidden).
3. Run the workflow again, leaving **set_live** as `test`. The build is described as
   `main <sha> (run N)` and is live on `test`.
4. Install it: in your Steam library, the game → Properties → Betas → `test`.

**set_live** is a dropdown: `test`, `default` or `none`. `default` is the branch every player
gets. Valve's SteamPipe documentation says a build script cannot set `default` live, so that
choice may fail at its last step. The upload has already happened by then, so the build is
in Steamworks and you can promote it on the Builds page by hand.

---

## What changed in the game to make this possible

- **`Program.cs`**: the game used to always compile the `.fbasic` files from the project
  folder at startup, which cannot work on a player's machine. That path is now Debug only.
  Release runs `GeneratedFade`, the program compiled at build time.
- **Saves**: the engine writes `prefs.json` (the saved game and the options) to the working
  directory. A macOS app starts in `/`, which cannot be written, so a Release build first
  moves to its own folder: `%APPDATA%\FishFishPop` on Windows,
  `~/Library/Application Support/FishFishPop` on macOS. Debug builds are unchanged.
- **`Fade.MonoGame.csproj`**: Release builds the content (`BuildGameContent`), has no console
  window (`WinExe`), and includes MonoGame, SDL and OpenAL in a publish. They were being left
  out of `dotnet publish` output before, so a published build could not start.

## Not verified

Checked on this Windows machine: a clean Release publish for both platforms, the published
Windows build starting up to its window, the asset scripts round-tripping a bundle made on
Windows and unpacked by the Linux script, and the macOS bundle layout.

Not checked, because it needs your accounts or other hardware:

- **The workflow has never run on GitHub.** The Linux steps follow `ci.yml` (Wine for
  shaders) and the beatboys workflow (steamcmd), but this game's 15 shaders, its font and its
  mp3s have not been built on Linux. The build-only run in step 6 is where that gets proven.
- **The macOS build has never been launched.** In particular: that it finds its content from
  inside the `.app`, that the OpenGL shaders behave on Apple Silicon, and that saving works.
- `build/push-assets.sh` (the non-Windows push) was not run; this machine has no `zip`.

## Things you may want later

- **Steam Cloud**: Auto-Cloud can sync `prefs.json` with no code. Windows root
  `WinAppDataRoaming`, macOS root `MacAppSupport`, subdirectory `FishFishPop` for both.
- **Steamworks API**: the game does not link the Steam API, so no achievements or rich
  presence, and it also runs when started outside Steam. Nothing above depends on it.
- **Intel Macs**: only Apple Silicon is built. Intel needs a second publish with
  `-r osx-x64` and its own bundle.
- **macOS icon**: the bundle has no `.icns`, so it shows the generic app icon in the Dock.
- **Size**: each build is about 200 MB. Roughly 70 MB is the .NET runtime and 120 MB is audio,
  which the content pipeline stores uncompressed. The publish also carries the test-runner
  DLLs (`FadeBasic.Testing`, `testhost`), which are harmless but could be made Debug only.

## Troubleshooting

**`build/assets.lock pins no bundle`**: step 1 is not finished, or the lock was not
committed.

**`HASH MISMATCH` from fetch-assets**: the zip in the bucket is not the one the lock names.
Run `push-assets` again and commit the lock.

**`Fade.MonoGame/Assets/Fish/Textures is missing`** on a local build: run `fetch-assets`.

**Login asks for a Steam Guard code in CI, or `Two-factor code mismatch`**: the saved login
did not carry over or has expired. Redo step 3, and make sure the second local login was
silent.

**CI login fails although the local one was silent**: the login was saved by Windows
steamcmd and is being used by Linux steamcmd. That normally works. If it does not, make
`config.vdf` on Linux instead (WSL works: `~/Steam/config/config.vdf`).

**Permission or access denied on upload**: the builder account lacks rights on the app.
Recheck step 3.2.

**`setlive` fails but the upload succeeded**: the branch does not exist yet, or the account
cannot set builds live. The build is in Steamworks; set it live by hand on the Builds page.

**Mac testers get Windows files, or nothing**: the macOS depot is not in the package, its OS
is not set to macOS, or the change was not published.

**The game starts with no art or sound on macOS**: the content is not in
`FishFishPop.app/Contents/Resources/Content`. The workflow checks for this; if it happens
anyway, MonoGame is looking somewhere else, and that is the first thing to look at.
